using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using NuGet.Services.Metadata.Catalog.Maintenance;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.Services.Metadata.Catalog.WarehouseIntegration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Services.Metrics.Core
{
    public class CatalogMetricsStorage : MetricsStorage
    {
        private const string IdParam = "@id";
        private const string NormalizedVersionParam = "@normalizedVersion";
        private static int CatalogWriterGate = 0; // When the CatalogWriterGate is 0, the catalog is open for writing. If 1, it is closed for writing
        private static ConcurrentQueue<JToken> CurrentStatsQueue = new ConcurrentQueue<JToken>();
        private static ConcurrentQueue<ConcurrentQueue<JToken>> StatsQueueOfQueues = new ConcurrentQueue<ConcurrentQueue<JToken>>();
        private Storage CatalogStorage { get; set; }
        private int CatalogPageSize { get; set; }
        private int CatalogItemPackageStatsCount { get; set; }

        private static readonly string PackageExistenceCheckQuery = @"
                SELECT		p.[Key],
                            p.Listed as " + CatalogPackageListed + @",
                            ISNULL(p.Title, '') as " + CatalogPackageTitle + @",
                            ISNULL(p.Description, '') as " + CatalogPackageDescription + @",
                            ISNULL(p.IconUrl, '')" + CatalogPackageIconUrl + @"
                FROM		Packages p
                INNER JOIN	PackageRegistrations pr
                ON          p.[PackageRegistrationKey] = pr.[Key]
                WHERE		Id = @id
                AND			NormalizedVersion = @normalizedVersion";

        private readonly SqlConnectionStringBuilder _cstr;
        private readonly int _commandTimeout;

        public CatalogMetricsStorage(IDictionary<string, string> appSettingDictionary)
        {
            string connectionString = MetricsAppSettings.GetSetting(appSettingDictionary, MetricsAppSettings.SqlConfigurationKey);
            int commandTimeout = MetricsAppSettings.TryGetIntSetting(appSettingDictionary, MetricsAppSettings.CommandTimeoutKey) ?? 0;

            _cstr = new SqlConnectionStringBuilder(connectionString);
            _commandTimeout = commandTimeout > 0 ? commandTimeout : 5;

            string catalogLocalDirectory = MetricsAppSettings.TryGetSetting(appSettingDictionary, MetricsAppSettings.CatalogLocalDirectoryKey);
            if(!String.IsNullOrEmpty(catalogLocalDirectory))
            {
                string catalogIndexUrl = MetricsAppSettings.GetSetting(appSettingDictionary, MetricsAppSettings.CatalogIndexUrlKey);
                CatalogStorage = new FileStorage(catalogIndexUrl, catalogLocalDirectory);
            }
            else
            {
                string catalogStorageAccountKey = MetricsAppSettings.GetSetting(appSettingDictionary, MetricsAppSettings.CatalogStorageAccountKey);
                string catalogPath = MetricsAppSettings.GetSetting(appSettingDictionary, MetricsAppSettings.CatalogPathKey);

                var catalogStorageAccount = CloudStorageAccount.Parse(catalogStorageAccountKey);
                var catalogDirectory = GetBlobDirectory(catalogStorageAccount, catalogPath);
                CatalogStorage = new AzureStorage(catalogDirectory);
            }

            CatalogPageSize = MetricsAppSettings.TryGetIntSetting(appSettingDictionary, MetricsAppSettings.CatalogPageSizeKey) ?? 500;
            CatalogItemPackageStatsCount = MetricsAppSettings.TryGetIntSetting(appSettingDictionary, MetricsAppSettings.CatalogItemPackageStatsCountKey) ?? 1000;
        }
        public override async Task AddPackageDownloadStatistics(JObject jObject)
        {
            var id = jObject[IdKey].ToString();
            // NEED to normalize
            var version = jObject[VersionKey].ToString();

            SqlDataReader reader = null;
            using (var connection = new SqlConnection(_cstr.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(PackageExistenceCheckQuery, connection);
                command.CommandTimeout = _commandTimeout;
                command.Parameters.AddWithValue(IdParam, id);
                command.Parameters.AddWithValue(NormalizedVersionParam, version);

                reader = await command.ExecuteReaderAsync();

                if(reader.Read())
                {
                    EnqueueStats(GetStatsCatalogItemRow(jObject, reader));
                }
                else
                {
                    Trace.TraceWarning("Package of id '{0}' and version '{1}' does not exist. Skipping...", id, version);
                }
            }
        }

        private static CloudBlobDirectory GetBlobDirectory(CloudStorageAccount account, string path)
        {
            var client = account.CreateCloudBlobClient();
            string[] segments = path.Split('/');
            string containerName;
            string prefix;

            if (segments.Length < 2)
            {
                // No "/" segments, so the path is a container and the catalog is at the root...
                containerName = path;
                prefix = String.Empty;
            }
            else
            {
                // Found "/" segments, but we need to get the first segment to use as the container...
                containerName = segments[0];
                prefix = String.Join("/", segments.Skip(1)) + "/";
            }

            var container = client.GetContainerReference(containerName);
            container.CreateIfNotExists();
            var dir = container.GetDirectoryReference(prefix);
            return dir;
        }

        private JToken GetStatsCatalogItemRow(JObject jObject, SqlDataReader reader)
        {
            JObject statsCatalogItemRow = new JObject();
            statsCatalogItemRow[CatalogDownloadTimestamp] = DateTime.UtcNow.ToString("O");
            statsCatalogItemRow[CatalogPackageId] = jObject[IdKey]; // At this point, package Id is present and not empty
            statsCatalogItemRow[CatalogPackageVersion] = jObject[VersionKey]; // At this point, package Version is present and not empty
            statsCatalogItemRow[CatalogDownloadUserAgent] = JTokenToString(jObject[UserAgentKey]);
            statsCatalogItemRow[CatalogDownloadOperation] = JTokenToString(jObject[OperationKey]);
            statsCatalogItemRow[CatalogDownloadDependentPackageId] = JTokenToString(jObject[DependentPackageKey]);
            statsCatalogItemRow[CatalogDownloadProjectTypes] = JTokenToString(jObject[ProjectGuidsKey]);
            statsCatalogItemRow[CatalogPackageTitle] = reader.GetString(reader.GetOrdinal(CatalogPackageTitle));
            statsCatalogItemRow[CatalogPackageDescription] = reader.GetString(reader.GetOrdinal(CatalogPackageDescription));
            statsCatalogItemRow[CatalogPackageIconUrl] = reader.GetString(reader.GetOrdinal(CatalogPackageIconUrl));
            statsCatalogItemRow[CatalogPackageListed] = reader.GetBoolean(reader.GetOrdinal(CatalogPackageListed));

            return statsCatalogItemRow;
        }

        private void EnqueueStats(JToken packageStatsCatalogItemRow)
        {
            Trace.WriteLine("AddToCatalog ThreadId:" + Environment.CurrentManagedThreadId);
            CurrentStatsQueue.Enqueue(packageStatsCatalogItemRow);
            if(CurrentStatsQueue.Count >= CatalogItemPackageStatsCount)
            {
                // It is possible that 2 or more threads have entered this point. In that case, 1 or more empty CurrentStatsQueue(s) will get enqueued to StatsQueueOfQueues
                // This is harmless, since during commit in CommitToCatalog, empty catalog items can be ignored easily
                StatsQueueOfQueues.Enqueue(Interlocked.Exchange(ref CurrentStatsQueue, new ConcurrentQueue<JToken>()));
                Task.Run(() => CommitToCatalog());
            }
        }

        private void CommitToCatalog()
        {
            // When the CatalogWriterGate is 0, the catalog is open for writing. If 1, it is closed for writing
            // Using Interlocked.Exchange, set value to 1 and close the gate, and check if the value returned is 0 to see if the gate was open
            // If the value returned is 1, that is, if the gate was already closed, do nothing
            // When 2 or more threads reach this point, while the gate is open, only 1 thread will enter. Rest will find that the gate is already closed and leave
            if (Interlocked.Equals(Interlocked.Exchange(ref CatalogWriterGate, 1), 0))
            {
                try
                {
                    using (CatalogWriter writer = new CatalogWriter(CatalogStorage, new CatalogContext(), CatalogPageSize))
                    {
                        ConcurrentQueue<JToken> headStatsQueue;
                        while (StatsQueueOfQueues.TryDequeue(out headStatsQueue))
                        {
                            if (headStatsQueue.Count == 0)
                            {
                                // An emtpy StatsQueue, ignore this one and go to the next one
                                continue;
                            }

                            JArray statsCatalogItem = new JArray();
                            foreach (JToken packageStats in headStatsQueue)
                            {
                                statsCatalogItem.Add(packageStats);
                            }

                            // Note that at this point, DateTime is already in UTC
                            string minDownloadTimestampString = statsCatalogItem[0][CatalogDownloadTimestamp].ToString();
                            DateTime minDownloadTimestamp = DateTime.Parse(minDownloadTimestampString, null, System.Globalization.DateTimeStyles.RoundtripKind);

                            string maxDownloadTimestampString = statsCatalogItem[statsCatalogItem.Count - 1][CatalogDownloadTimestamp].ToString();
                            DateTime maxDownloadTimestamp = DateTime.Parse(minDownloadTimestampString, null, System.Globalization.DateTimeStyles.RoundtripKind);
                            writer.Add(new StatisticsCatalogItem(statsCatalogItem,
                                minDownloadTimestamp,
                                maxDownloadTimestamp));
                            writer.Commit().Wait();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
                Interlocked.Exchange(ref CatalogWriterGate, 0);
            }
            else
            {
                Trace.WriteLine("Another thread is committing to catalog. Skipping");
            }
        }
    }
}
