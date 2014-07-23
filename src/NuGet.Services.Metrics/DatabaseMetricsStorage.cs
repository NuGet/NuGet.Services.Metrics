using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Services.Metrics
{
    public class DatabaseMetricsStorage : MetricsStorage
    {
        private const string UnknownIPAddress = "unknown";
        private const string NullString = "null";
        private const string SqlStringFormat = "'{0}'";
        private const string InsertQueryStringFormat = @"INSERT INTO PackageStatistics
(PackageKey, IPAddress, UserAgent, Operation, DependentPackage, ProjectGuids)
VALUES(
(SELECT		[Key]
FROM		Packages
WHERE		[PackageRegistrationKey] IN
(SELECT		[Key]
FROM		PackageRegistrations
WHERE		Id = {0})
AND			NormalizedVersion = {1})
, {2}, {3}, {4}, {5}, {6})";

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

            var insertQuery = String.Format(InsertQueryStringFormat,
                SqlStringify(id),
                SqlStringify(version),
                SqlStringify(ipAddress),
                SqlStringify(userAgent),
                SqlStringify(operation),
                SqlStringify(dependentPackage),
                SqlStringify(projectGuids));

            using (var connection = new SqlConnection(_cstr.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(insertQuery, connection);
                await command.ExecuteNonQueryAsync();
                connection.Close();
            }
        }

        private string SqlStringify(string param)
        {
            if (param == null)
                return NullString;

            return String.Format(SqlStringFormat, param);
        }
    }
}
