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
        private const string IdParam = "@id";
        private const string NormalizedVersionParam = "@normalizedVersion";
        private const string UserAgentParam = "@userAgent";
        private const string OperationParam = "@operation";
        private const string DependentPackageParam = "@dependentPackage";
        private const string ProjectGuidsParam = "@projectGuids";

        private static readonly string InsertQuery = String.Format(@"INSERT INTO PackageStatistics
(PackageKey, IPAddress, UserAgent, Operation, DependentPackage, ProjectGuids)
VALUES(
(SELECT		[Key]
FROM		Packages
WHERE		[PackageRegistrationKey] IN
(SELECT		[Key]
FROM		PackageRegistrations
WHERE		Id = {0})
AND			NormalizedVersion = {1}), 'unknown', {2}, {3}, {4}, {5})",
                                 IdParam,
                                 NormalizedVersionParam,
                                 UserAgentParam,
                                 OperationParam,
                                 DependentPackageParam,
                                 ProjectGuidsParam);

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

            var userAgent = JTokenToString(jObject[UserAgentKey]);
            var operation = JTokenToString(jObject[OperationKey]);
            var dependentPackage = JTokenToString(jObject[DependentPackageKey]);
            var projectGuids = JTokenToString(jObject[ProjectGuidsKey]);

            using (var connection = new SqlConnection(_cstr.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(InsertQuery, connection);
                command.Parameters.AddWithValue(IdParam, id);
                command.Parameters.AddWithValue(NormalizedVersionParam, version);
                command.Parameters.AddWithValue(UserAgentParam, GetSqlValue(userAgent));
                command.Parameters.AddWithValue(OperationParam, GetSqlValue(operation));
                command.Parameters.AddWithValue(DependentPackageParam, GetSqlValue(dependentPackage));
                command.Parameters.AddWithValue(ProjectGuidsParam, GetSqlValue(projectGuids));

                await command.ExecuteNonQueryAsync();
                connection.Close();
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
