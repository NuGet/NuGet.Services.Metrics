using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Metrics
{
    public class Startup
    {
        private PackageStatsHandler _packageStatsHandler;
        public void Configuration(IAppBuilder appBuilder)
        {
            _packageStatsHandler = new PackageStatsHandler();
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
