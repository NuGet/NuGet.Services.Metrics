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
        private static ConcurrentQueue<JToken> CurrentStatsQueue = new ConcurrentQueue<JToken>();
        private static ConcurrentQueue<ConcurrentQueue<JToken>> StatsQueueOfQueues = new ConcurrentQueue<ConcurrentQueue<JToken>>();
        private Storage CatalogStorage { get; set; }
        private int CatalogPageSize { get; set; }
        private int CatalogItemPackageStatsCount { get; set; }

        private const string PackageExistenceCheckQuery = @"
                SELECT		p.[Key],
                            p.Listed,
                            ISNULL(p.Title, ''),
                            ISNULL(p.Description, ''),
                            ISNULL(p.IconUrl, '')
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
                    await EnqueueStats(GetJToken(jObject, reader));
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

        private JToken GetJToken(JObject jObject, SqlDataReader reader)
        {
            var userAgent = JTokenToString(jObject[UserAgentKey]);
            var operation = JTokenToString(jObject[OperationKey]);
            var dependentPackage = JTokenToString(jObject[DependentPackageKey]);
            var projectGuids = JTokenToString(jObject[ProjectGuidsKey]);

            JArray row = new JArray();
            row.Add(DateTime.UtcNow.ToString("O"));
            row.Add(reader.GetInt32(0));
            row.Add(jObject[IdKey].ToString());
            row.Add(jObject[IdKey].ToString());
            row.Add(userAgent);
            row.Add(operation);
            row.Add(dependentPackage);
            row.Add(projectGuids);
            row.Add(reader.GetBoolean(1));
            row.Add(reader.GetString(2));
            row.Add(reader.GetString(3));
            row.Add(reader.GetString(4));

            return row;
        }

        private async Task EnqueueStats(JToken row)
        {
            Trace.WriteLine("AddToCatalog ThreadId:" + Environment.CurrentManagedThreadId);
            CurrentStatsQueue.Enqueue(row);
            if(CurrentStatsQueue.Count >= CatalogItemPackageStatsCount)
            {
                StatsQueueOfQueues.Enqueue(Interlocked.Exchange(ref CurrentStatsQueue, new ConcurrentQueue<JToken>()));
            }
        }

        private void CatalogCommitRunner()
        {
            Trace.WriteLine("CatalogCommitRunner ThreadId:" + Environment.CurrentManagedThreadId);
            using (CatalogWriter writer = new CatalogWriter(CatalogStorage, new CatalogContext(), CatalogPageSize))
            {
                while(CurrentStatsQueue != null)
                {
                    Thread.Sleep(1000);
                    CommitToCatalog(writer);
                }
            }
        }

        private void CommitToCatalog(CatalogWriter writer)
        {
            try
            {
                ConcurrentQueue<JToken> headStatsQueue;
                while (StatsQueueOfQueues.TryDequeue(out headStatsQueue))
                {
                    JArray statsCatalogItem = new JArray();
                    foreach (JToken packageStats in headStatsQueue)
                    {
                        statsCatalogItem.Add(packageStats);
                    }
                    // Note that at this point, DateTime is already in UTC
                    string minDownloadTimestampString = statsCatalogItem[0][0].ToString();
                    DateTime minDownloadTimestamp = DateTime.Parse(minDownloadTimestampString, null, System.Globalization.DateTimeStyles.RoundtripKind);

                    string maxDownloadTimestampString = statsCatalogItem[statsCatalogItem.Count - 1][0].ToString();
                    DateTime maxDownloadTimestamp = DateTime.Parse(minDownloadTimestampString, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    writer.Add(new StatisticsCatalogItem(statsCatalogItem,
                        "PackageStats",
                        minDownloadTimestamp,
                        maxDownloadTimestamp));
                    writer.Commit().Wait();
                    Thread.Sleep(1000);
                }

            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }
    }
}
