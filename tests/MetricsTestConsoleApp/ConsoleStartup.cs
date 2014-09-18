using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Services.Metrics.Core;
using Owin;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;

namespace MetricsTestConsoleApp
{
    internal class ConsoleStartup
    {
        private PackageStatsHandler _packageStatsHandler;
        private const string ConnectionString = "Data Source=(LocalDB)\\v11.0;Integrated Security=SSPI;Initial Catalog=NuGetGallery";
        private const int CommandTimeout = 5;
        private const string CatalogStorageAccount = @"UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://localhost:8000";
        private const string CatalogPath = "catalogmetricsstorage";
        private const string CatalogIndexUrl = "http://localhost:8000/CatalogMetricsStorage";
        private const bool IsLocalCatalog = true;
        private const int CatalogItemPackageStatsCount = 2;
        private const bool ShouldUseDBAndCatalog = true;

        public void Configuration(IAppBuilder appBuilder)
        {
            IDictionary<string, string> appSettingDictionary = new Dictionary<string, string>();
            appSettingDictionary.Add(MetricsAppSettings.SqlConfigurationKey, ConnectionString);
            appSettingDictionary.Add(MetricsAppSettings.CommandTimeoutKey, CommandTimeout.ToString());
            appSettingDictionary.Add(MetricsAppSettings.IsLocalCatalogKey, IsLocalCatalog.ToString());

            if (IsLocalCatalog)
            {
                appSettingDictionary.Add(MetricsAppSettings.CatalogIndexUrlKey, CatalogIndexUrl);
            }
            else
            {
                appSettingDictionary.Add(MetricsAppSettings.CatalogStorageAccountKey, CatalogStorageAccount.ToString());
                appSettingDictionary.Add(MetricsAppSettings.CatalogPathKey, CatalogPath);
            }
            appSettingDictionary.Add(MetricsAppSettings.CatalogItemPackageStatsCountKey, CatalogItemPackageStatsCount.ToString());
            appSettingDictionary.Add(MetricsAppSettings.ShouldUseDBAndCatalog, ShouldUseDBAndCatalog.ToString());

            _packageStatsHandler = new PackageStatsHandler(appSettingDictionary);
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
