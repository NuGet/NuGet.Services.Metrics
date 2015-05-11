// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.Metrics.Core.Tests
{
    public class MetricsTests
    {
        const string IdKey = "id";
        const string VersionKey = "version";
        const string IPAddressKey = "ipAddress";
        const string UserAgentKey = "userAgent";
        const string OperationKey = "operation";
        const string DependentPackageKey = "dependentPackage";
        const string ProjectGuidsKey = "projectGuids";
        const string MetricsService = @"http://localhost:12345";
        private Uri ServiceRoot = null;

        public MetricsTests()
        {
            var root = Environment.GetEnvironmentVariable("NUGET_TEST_SERVICEROOT");
            if(String.IsNullOrEmpty(root))
            {
                root = MetricsService;
            }

            ServiceRoot = new Uri(root);
        }

        JObject GetJObject(string id, string version, string ipAddress, string userAgent, string operation, string dependentPackage, string projectGuids)
        {
            var jObject = new JObject();
            jObject.Add(IdKey, id);
            jObject.Add(VersionKey, version);
            if (!String.IsNullOrEmpty(ipAddress)) jObject.Add(IPAddressKey, ipAddress);
            if (!String.IsNullOrEmpty(userAgent)) jObject.Add(UserAgentKey, userAgent);
            if (!String.IsNullOrEmpty(operation)) jObject.Add(OperationKey, operation);
            if (!String.IsNullOrEmpty(dependentPackage)) jObject.Add(DependentPackageKey, dependentPackage);
            if (!String.IsNullOrEmpty(projectGuids)) jObject.Add(ProjectGuidsKey, projectGuids);

            return jObject;
        }

        async Task<HttpResponseMessage> RunScenario(JToken jToken, string uri,
            bool nonJSONRequest = false, bool nonPostRequest = false)
        {            
            Console.WriteLine("Requesting response...");
            HttpResponseMessage response;
            using (var httpClient = new HttpClient()
                                    {
                                        BaseAddress = ServiceRoot
                                    })
            {
                if (nonPostRequest)
                {
                    response = await httpClient.GetAsync(uri);
                }
                else
                {
                    response = await httpClient.PostAsync(uri, new StringContent(nonJSONRequest ? "blah" : jToken.ToString(), Encoding.Default, "application/json"));
                } 
            }
            Console.WriteLine(response.StatusCode);
            Console.WriteLine("Received response");
            return response;
        }

        [Theory]
        [InlineData("Root endpoint", "/", HttpStatusCode.OK, false, false, 1)]
        [InlineData("Non existent endpoint", "/DoesNotExist", HttpStatusCode.NotFound, false, false, 1)]
        [InlineData("Request is not JSON", "/DownloadEvent", HttpStatusCode.BadRequest, true, false, 1)]
        [InlineData("Request is not a POST", "/DownloadEvent", HttpStatusCode.MethodNotAllowed, false, true, 1)]
        [InlineData("Valid downloadEvent request", "/DownloadEvent", HttpStatusCode.Accepted, false, false, 1)]
        [InlineData("Multiple valid downloadEvent requests", "/DownloadEvent", HttpStatusCode.Accepted, false, false, 5)]
        public async Task RunScenario(string scenario, string uri, HttpStatusCode expected, bool nonJSONRequest, bool nonPostRequest, int numberOfEvents)
        {
            var id = "EntityFramework";
            var version = "5.0.0";
            string ipAddress = null;
            string userAgent = "Functional Tests Runner";
            string operation = "Install";
            string dependentPackage = null;
            string projectGuids = null;

            Console.WriteLine("Running scenario {0} on host: {1}", scenario, ServiceRoot.AbsoluteUri);
            JToken jToken = null;
            if (numberOfEvents == 1)
            {
                jToken = GetJObject(id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids);
            }
            else
            {
                JArray jArray = new JArray();
                for(int i = 0; i < numberOfEvents; i++)
                {
                    jArray.Add(GetJObject(id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids));
                }
                jToken = jArray;
            }

            var actualResponse = await RunScenario(jToken, uri, nonJSONRequest: nonJSONRequest, nonPostRequest: nonPostRequest);
            Assert.Equal(expected, actualResponse.StatusCode) ;
            if (actualResponse.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                var actualAllowHeaderValue = actualResponse.Content.Headers.GetValues(HttpResponseHeader.Allow.ToString()).SingleOrDefault();
                Assert.True(String.Equals(actualAllowHeaderValue, HttpMethod.Post.ToString(), StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}