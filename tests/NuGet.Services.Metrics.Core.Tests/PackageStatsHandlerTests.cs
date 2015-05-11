// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.Metrics.Core.Tests
{
    public class PackageStatsHandlerTests
    {
        private const string ConnectionString = "Data Source=(LocalDB)\\v11.0;Integrated Security=SSPI;Initial Catalog=NuGetGallery";
        private const int CommandTimeout = 5;
        private const string CatalogStorageAccount = @"UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://localhost:8000";
        private const string CatalogPath = "catalogmetricsstorage";
        private const string CatalogLocalDirectory = @"c:\data\site\catalogmetricsstorage";
        private const string CatalogBaseAddress = "http://localhost:8000/CatalogMetricsStorage";
        private const int CatalogItemPackageStatsCount = 2;
        private const bool ShouldUseDB = true;
        private const bool ShouldUseCatalog = true;

        public PackageStatsHandlerTests()
        {
            Environment.SetEnvironmentVariable(CatalogMetricsStorage.WEBSITE_INSTANCE_ID, "A1B2C3D4");
        }

        [Fact]
        public void PackageStatsHandlerConfigNothing()
        {
            Assert.Throws(typeof(ArgumentException), () => { new PackageStatsHandler(new Dictionary<string, string>()); });
        }

        [Fact]
        public void PackageStatsHandlerConfigDBOnly()
        {
            IDictionary<string, string> appSettingDictionary = new Dictionary<string, string>();

            appSettingDictionary.Add(MetricsAppSettings.ShouldUseDB, ShouldUseDB.ToString());
            appSettingDictionary.Add(MetricsAppSettings.SqlConfigurationKey, ConnectionString);
            appSettingDictionary.Add(MetricsAppSettings.CommandTimeoutKey, CommandTimeout.ToString());

            new PackageStatsHandler(appSettingDictionary);
        }

        [Fact]
        public void PackageStatsHandlerConfigDBWithoutNecessary()
        {
            IDictionary<string, string> appSettingDictionary = new Dictionary<string, string>();

            appSettingDictionary.Add(MetricsAppSettings.ShouldUseDB, ShouldUseDB.ToString());
            appSettingDictionary.Add(MetricsAppSettings.CommandTimeoutKey, CommandTimeout.ToString());

            Assert.Throws(typeof(ArgumentException), () => { new PackageStatsHandler(appSettingDictionary); });
        }

        [Fact]
        public void PackageStatsHandlerConfigLocalCatalogOnly()
        {
            IDictionary<string, string> appSettingDictionary = new Dictionary<string, string>();

            appSettingDictionary.Add(MetricsAppSettings.ShouldUseCatalog, ShouldUseCatalog.ToString());
            appSettingDictionary.Add(MetricsAppSettings.SqlConfigurationKey, ConnectionString);
            appSettingDictionary.Add(MetricsAppSettings.CommandTimeoutKey, CommandTimeout.ToString());
            appSettingDictionary.Add(MetricsAppSettings.CatalogLocalDirectoryKey, CatalogLocalDirectory);
            appSettingDictionary.Add(MetricsAppSettings.CatalogBaseAddressKey, CatalogBaseAddress);

            new PackageStatsHandler(appSettingDictionary);
        }

        [Fact]
        public void PackageStatsHandlerConfigLocalCatalogWithoutNecessary()
        {
            IDictionary<string, string> appSettingDictionary = new Dictionary<string, string>();

            appSettingDictionary.Add(MetricsAppSettings.ShouldUseCatalog, ShouldUseCatalog.ToString());
            appSettingDictionary.Add(MetricsAppSettings.SqlConfigurationKey, ConnectionString);
            appSettingDictionary.Add(MetricsAppSettings.CommandTimeoutKey, CommandTimeout.ToString());
            appSettingDictionary.Add(MetricsAppSettings.CatalogLocalDirectoryKey, CatalogLocalDirectory);

            Assert.Throws(typeof(ArgumentException), () => { new PackageStatsHandler(appSettingDictionary); });
        }

        [Fact]
        public void PackageStatsHandlerConfigAzureCatalogOnly()
        {
            IDictionary<string, string> appSettingDictionary = new Dictionary<string, string>();

            appSettingDictionary.Add(MetricsAppSettings.ShouldUseCatalog, ShouldUseCatalog.ToString());
            appSettingDictionary.Add(MetricsAppSettings.SqlConfigurationKey, ConnectionString);
            appSettingDictionary.Add(MetricsAppSettings.CommandTimeoutKey, CommandTimeout.ToString());
            appSettingDictionary.Add(MetricsAppSettings.CatalogStorageAccountKey, CatalogStorageAccount);
            appSettingDictionary.Add(MetricsAppSettings.CatalogPathKey, CatalogPath);

            new PackageStatsHandler(appSettingDictionary);
        }

        [Fact]
        public void PackageStatsHandlerConfigAzureCatalogWithoutNecessary()
        {
            IDictionary<string, string> appSettingDictionary = new Dictionary<string, string>();

            appSettingDictionary.Add(MetricsAppSettings.ShouldUseCatalog, ShouldUseCatalog.ToString());
            appSettingDictionary.Add(MetricsAppSettings.SqlConfigurationKey, ConnectionString);
            appSettingDictionary.Add(MetricsAppSettings.CommandTimeoutKey, CommandTimeout.ToString());
            appSettingDictionary.Add(MetricsAppSettings.CatalogPathKey, CatalogPath);

            Assert.Throws(typeof(ArgumentException), () => { new PackageStatsHandler(appSettingDictionary); });
        }

        [Fact]
        public void PackageStatsHandlerConfigBothDBAndCatalog()
        {
            IDictionary<string, string> appSettingDictionary = new Dictionary<string, string>();

            appSettingDictionary.Add(MetricsAppSettings.ShouldUseDB, ShouldUseDB.ToString());
            appSettingDictionary.Add(MetricsAppSettings.ShouldUseCatalog, ShouldUseCatalog.ToString());
            appSettingDictionary.Add(MetricsAppSettings.SqlConfigurationKey, ConnectionString);
            appSettingDictionary.Add(MetricsAppSettings.CommandTimeoutKey, CommandTimeout.ToString());
            appSettingDictionary.Add(MetricsAppSettings.CatalogLocalDirectoryKey, CatalogLocalDirectory);
            appSettingDictionary.Add(MetricsAppSettings.CatalogBaseAddressKey, CatalogBaseAddress);
            appSettingDictionary.Add(MetricsAppSettings.CatalogStorageAccountKey, CatalogStorageAccount);
            appSettingDictionary.Add(MetricsAppSettings.CatalogPathKey, CatalogPath);
            appSettingDictionary.Add(MetricsAppSettings.CatalogItemPackageStatsCountKey, CatalogItemPackageStatsCount.ToString());

            new PackageStatsHandler(appSettingDictionary);
        }
    }
}
