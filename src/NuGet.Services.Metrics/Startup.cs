using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.WindowsAzure.ServiceRuntime;
using NuGet.Services.Metrics.Core;
using Owin;

namespace NuGet.Services.Metrics
{
    internal class Startup
    {
        private PackageStatsHandler _packageStatsHandler;
        private const string SqlConfigurationKey = "Metrics.SqlServer";
        public void Configuration(IAppBuilder appBuilder)
        {
            string connectionString = RoleEnvironment.GetConfigurationSettingValue(SqlConfigurationKey);
            _packageStatsHandler = new PackageStatsHandler(connectionString);
            appBuilder.Run(Invoke);
        }

        private async Task Invoke(IOwinContext context)
        {
            var requestUri = context.Request.Uri;
            Trace.TraceInformation("Request received : {0}", requestUri.AbsoluteUri);

            await _packageStatsHandler.Invoke(context);
            Trace.TraceInformation("Request accepted. Processing...");
        }
    }
}
