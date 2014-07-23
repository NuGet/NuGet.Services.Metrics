using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json.Linq;

namespace NuGet.Services.Metrics
{
    public class DatabaseMetricsStorage : MetricsStorage
    {
        private const string UnknownIPAddress = "unknown";
        private const string InsertQuery = @"INSERT INTO PackageStatistics
(PackageKey, IPAddress, UserAgent, Operation, DependentPackage, ProjectGuids)
VALUES(@packageKey, @ipAddress, @userAgent, @operation, @dependentPackage, @projectGuids)";

        private const string PackageKeyGetQuery = @"SELECT		[Key]
FROM		Packages
WHERE		[PackageRegistrationKey] IN
(SELECT		[Key]
FROM		PackageRegistrations
WHERE		Id = @id)
AND			NormalizedVersion = @normalizedVersion";

        private readonly SqlConnectionStringBuilder _cstr;

        public DatabaseMetricsStorage(string connectionString)
        {
            _cstr = new SqlConnectionStringBuilder(connectionString);
        }

        public override async Task AddPackageDownloadStatistics(JObject jObject)
        {
            var id = jObject[IdKey].ToString();
            // NEED to normalize
            var version = jObject[VersionKey].ToString();

            var ipAddress = UnknownIPAddress; // Always store "unknown" as the IPAddress
            var userAgent = JTokenToString(jObject[UserAgentKey]);
            var operation = JTokenToString(jObject[OperationKey]);
            var dependentPackage = JTokenToString(jObject[DependentPackageKey]);
            var projectGuids = JTokenToString(jObject[ProjectGuidsKey]);

            using (var connection = new SqlConnection(_cstr.ConnectionString))
            {
                await connection.OpenAsync();
                var result = await connection.QueryAsync<int>(PackageKeyGetQuery, new { id = id, normalizedVersion = version });
                int packageKey = result.SingleOrDefault();
                if(packageKey != 0)
                {
                    await connection.QueryAsync<int>(InsertQuery, new { packageKey = packageKey, ipAddress = ipAddress, userAgent = userAgent, operation = operation, dependentPackage = dependentPackage, projectGuids = projectGuids});
                }
                connection.Close();
            }
        }
    }
}
