using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Services.Metrics.Core;
using Owin;

namespace MetricsTestConsoleApp
{
    internal class ConsoleStartup
    {
        private PackageStatsHandler _packageStatsHandler;
        private const string ConnectionString = "Data Source=(LocalDB)\\v11.0;Integrated Security=SSPI;Initial Catalog=NuGetGallery";
        public void Configuration(IAppBuilder appBuilder)
        {            
            _packageStatsHandler = new PackageStatsHandler(ConnectionString);
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
