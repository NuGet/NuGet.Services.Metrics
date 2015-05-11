// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
        private const string CatalogPath = "catalogmetricsstorage-consolemetrics";
        private const string CatalogLocalDirectory = @"c:\data\site\catalogmetricsstorage-consolemetrics";
        private const string CatalogIndexUrl = "http://localhost:8000/CatalogMetricsStorage";
        private const int CatalogItemPackageStatsCount = 100;
        private const int CatalogPageSize = 10;
        private const bool ShouldUseDB = false;
        private const bool ShouldUseCatalog = true;

        public void Configuration(IAppBuilder appBuilder)
        {
            string websiteInstanceId = Environment.GetEnvironmentVariable(CatalogMetricsStorage.WEBSITE_INSTANCE_ID);
            if(String.IsNullOrEmpty(websiteInstanceId))
            {
                Environment.SetEnvironmentVariable(CatalogMetricsStorage.WEBSITE_INSTANCE_ID, "A1B2C3D4");
            }

            IDictionary<string, string> appSettingDictionary = new Dictionary<string, string>();

            appSettingDictionary.Add(MetricsAppSettings.ShouldUseDB, ShouldUseDB.ToString());
            appSettingDictionary.Add(MetricsAppSettings.ShouldUseCatalog, ShouldUseCatalog.ToString());
            appSettingDictionary.Add(MetricsAppSettings.SqlConfigurationKey, ConnectionString);
            appSettingDictionary.Add(MetricsAppSettings.CommandTimeoutKey, CommandTimeout.ToString());
            //appSettingDictionary.Add(MetricsAppSettings.CatalogLocalDirectoryKey, CatalogLocalDirectory);
            appSettingDictionary.Add(MetricsAppSettings.CatalogBaseAddressKey, CatalogIndexUrl);
            appSettingDictionary.Add(MetricsAppSettings.CatalogStorageAccountKey, CatalogStorageAccount);
            appSettingDictionary.Add(MetricsAppSettings.CatalogPathKey, CatalogPath);
            appSettingDictionary.Add(MetricsAppSettings.CatalogPageSizeKey, CatalogPageSize.ToString());
            appSettingDictionary.Add(MetricsAppSettings.CatalogItemPackageStatsCountKey, CatalogItemPackageStatsCount.ToString());

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
