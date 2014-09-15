using Newtonsoft.Json.Linq;
using NuGet.Services.Metadata.Catalog.Maintenance;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.Services.Metadata.Catalog.WarehouseIntegration;
using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NuGet.Services.Metrics.Core
{
    public class CatalogMetricsStorage : MetricsStorage
    {
        private const string IdParam = "@id";
        private const string NormalizedVersionParam = "@normalizedVersion";
        private static readonly ConcurrentQueue<JToken> StatsQueue = new ConcurrentQueue<JToken>();
        private Storage CatalogStorage { get; set; }

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
        public CatalogMetricsStorage(string connectionString, int commandTimeout, string catalogIndexUrl, bool isLocalCatalog)
        {
            _cstr = new SqlConnectionStringBuilder(connectionString);
            _commandTimeout = commandTimeout > 0 ? commandTimeout : 5;
            if(isLocalCatalog)
            {
                CatalogStorage = new FileStorage(catalogIndexUrl, @"c:\data\site\CatalogMetricsStorage");
            }
            else
            {
                throw new NotImplementedException("Only local Catalog has been implemented");
            }
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
                    await AddToCatalog(GetJToken(jObject, reader));
                }
                else
                {
                    Trace.TraceWarning("Package of id '{0}' and version '{1}' does not exist. Skipping...", id, version);
                }
            }
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

        private async Task AddToCatalog(JToken row)
        {
            StatsQueue.Enqueue(row);
            if(StatsQueue.Count >= 10)
            {
                JArray statsCatalogItem = new JArray();

                JToken packageStats;
                while(StatsQueue.TryDequeue(out packageStats))
                {
                    statsCatalogItem.Add(packageStats);
                }

                // Note that at this point, DateTime is already in UTC
                string minDownloadTimestampString = statsCatalogItem[0][0].ToString();
                DateTime minDownloadTimestamp = DateTime.Parse(minDownloadTimestampString, null, System.Globalization.DateTimeStyles.RoundtripKind);

                string maxDownloadTimestampString = statsCatalogItem[statsCatalogItem.Count - 1][0].ToString();
                DateTime maxDownloadTimestamp = DateTime.Parse(minDownloadTimestampString, null, System.Globalization.DateTimeStyles.RoundtripKind);

                using (CatalogWriter writer = new CatalogWriter(CatalogStorage, new CatalogContext(), 500))
                {
                    writer.Add(new StatisticsCatalogItem(statsCatalogItem,
                        "PackageStats",
                        minDownloadTimestamp,
                        maxDownloadTimestamp));
                    await writer.Commit();
                }
            }
        }
    }
}
