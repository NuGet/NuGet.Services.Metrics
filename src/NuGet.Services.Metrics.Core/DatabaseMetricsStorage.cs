using System;
using System.Data.SqlClient;
using System.Diagnostics;
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
        private readonly int _commandRetries;

        public DatabaseMetricsStorage(string connectionString, int commandTimeout, int commandRetries)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("DB connectionstring is not present in the configuration");
            }
            _cstr = new SqlConnectionStringBuilder(connectionString);
            _commandTimeout = commandTimeout > 0 ? commandTimeout : 5;
            _commandRetries = commandRetries > 0 ? commandRetries : 10;
            Trace.TraceInformation(String.Format("Server: {0}, InitialCatalog: {1}, ConnectionTimeout: {2}", _cstr.DataSource, _cstr.InitialCatalog, _cstr.ConnectTimeout));
            Trace.TraceInformation(String.Format("Command timeout from configuration: {0}. Command timeout actually used: {1}", commandTimeout, _commandTimeout));
            Trace.TraceInformation(String.Format("Command retries from configuration: {0}. Command retries actually used: {1}", commandRetries, _commandRetries));
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

            bool retry = true;
            int commandRetries = 0;
            while (retry)
            {
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
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        retry = false;
                    }
                    catch (SqlException ex)
                    {
                        // SqlException.Number is equal to -2 if the exception is a timeout
                        // So, if ex.Number != -2, it is not a timeout, throw. Don't retry
                        // Also, don't retry if the number of retries has exceeded the limit
                        if (ex.Number != -2 || commandRetries++ >= _commandRetries)
                            throw ex;

                        Trace.TraceError(String.Format("SqlException timeout encountered. Message : {0}, Command timeout in place: {1}. Retrying... Retry Attempt : {2}", ex.Message, _commandTimeout, commandRetries));
                    }
                }
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
