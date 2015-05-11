// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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

        // Package Download Statistics Catalog Item Row Keys. Each StatisticsCatalogItem has 1000 rows. Each row being a package stats
        public static readonly string CatalogDownloadTimestamp = "downloadTimestamp";
        public static readonly string CatalogPackageId = "packageId";
        public static readonly string CatalogPackageVersion = "packageVersion";
        public static readonly string CatalogDownloadUserAgent = "downloadUserAgent";
        public static readonly string CatalogDownloadOperation = "downloadOperation";
        public static readonly string CatalogDownloadDependentPackageId = "downloadDependentPackageId";
        public static readonly string CatalogDownloadProjectTypes = "downloadProjectTypes";
        public static readonly string CatalogPackageTitle = "packageTitle";
        public static readonly string CatalogPackageDescription = "packageDescription";
        public static readonly string CatalogPackageIconUrl = "packageIconUrl";
        public static readonly string CatalogPackageListed = "packageListed";

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
