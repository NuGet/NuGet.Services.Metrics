using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Services.Metrics.Core
{
    public abstract class MetricsStorage
    {
        // Package Download Statistics Keys
        public static readonly string IdKey = "id";
        public static readonly string VersionKey = "version";
        public static readonly string IPAddressKey = "ipAddress";
        public static readonly string UserAgentKey = "userAgent";
        public static readonly string OperationKey = "operation";
        public static readonly string DependentPackageKey = "dependentPackage";
        public static readonly string ProjectGuidsKey = "projectGuids";

        public abstract Task AddPackageDownloadStatistics(JObject jObject);

        protected string JTokenToString(JToken token)
        {
            if (token == null)
                return String.Empty;

            var str = token.ToString();
            return String.IsNullOrEmpty(str) ? String.Empty : str;
        }
    }
}
