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
        private const int CommandTimeout = 5;
        public void Configuration(IAppBuilder appBuilder)
        {            
            _packageStatsHandler = new PackageStatsHandler(ConnectionString, CommandTimeout);
            appBuilder.Run(Invoke);
        }

        private async Task Invoke(IOwinContext context)
        {
            var requestUri = context.Request.Uri;
            Trace.WriteLine("Request received : " + requestUri.AbsoluteUri);

            await _packageStatsHandler.Invoke(context);
            Trace.WriteLine("Request accepted. Processing...");
        }
    }
}
