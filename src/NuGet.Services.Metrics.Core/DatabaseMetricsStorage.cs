using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Services.Metrics.Core
{
    public class DatabaseMetricsStorage : MetricsStorage
    {
        private const string IdParam = "@id";
        private const string NormalizedVersionParam = "@normalizedVersion";
        private const string UserAgentParam = "@userAgent";
        private const string OperationParam = "@operation";
        private const string DependentPackageParam = "@dependentPackage";
        private const string ProjectGuidsParam = "@projectGuids";

        private static readonly string InsertQuery = @"INSERT INTO PackageStatistics
(PackageKey, IPAddress, UserAgent, Operation, DependentPackage, ProjectGuids)
VALUES(
(SELECT		p.[Key]
FROM		Packages p
INNER JOIN	PackageRegistrations pr
ON          p.[PackageRegistrationKey] = pr.[Key]
WHERE		Id = @id
AND			NormalizedVersion = @normalizedVersion), 'unknown', @userAgent, @operation, @dependentPackage, @projectGuids)";

        private readonly SqlConnectionStringBuilder _cstr;
        private readonly int _commandTimeout;

        public DatabaseMetricsStorage(string connectionString, int commandTimeout)
        {
            _cstr = new SqlConnectionStringBuilder(connectionString);
            _commandTimeout = commandTimeout > 0 ? commandTimeout : 5;
        }

        public override async Task AddPackageDownloadStatistics(JObject jObject)
        {
            var id = jObject[IdKey].ToString();
            // NEED to normalize
            var version = jObject[VersionKey].ToString();

            var userAgent = JTokenToString(jObject[UserAgentKey]);
            var operation = JTokenToString(jObject[OperationKey]);
            var dependentPackage = JTokenToString(jObject[DependentPackageKey]);
            var projectGuids = JTokenToString(jObject[ProjectGuidsKey]);

            using (var connection = new SqlConnection(_cstr.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(InsertQuery, connection);
                command.CommandTimeout = _commandTimeout;
                command.Parameters.AddWithValue(IdParam, id);
                command.Parameters.AddWithValue(NormalizedVersionParam, version);
                command.Parameters.AddWithValue(UserAgentParam, GetSqlValue(userAgent));
                command.Parameters.AddWithValue(OperationParam, GetSqlValue(operation));
                command.Parameters.AddWithValue(DependentPackageParam, GetSqlValue(dependentPackage));
                command.Parameters.AddWithValue(ProjectGuidsParam, GetSqlValue(projectGuids));

                await command.ExecuteNonQueryAsync();
            }
        }

        private object GetSqlValue(string param)
        {
            if (String.IsNullOrEmpty(param))
            {
                return DBNull.Value;
            }
            else
            {
                return param;
            }
        }
    }
}
