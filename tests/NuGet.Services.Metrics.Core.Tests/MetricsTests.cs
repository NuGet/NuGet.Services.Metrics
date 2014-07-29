using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Metrics.Core.Tests
{
    public class MetricsTests
    {
        static string IdKey = "id";
        static string VersionKey = "version";
        static string IPAddressKey = "ipAddress";
        static string UserAgentKey = "userAgent";
        static string OperationKey = "operation";
        static string DependentPackageKey = "dependentPackage";
        static string ProjectGuidsKey = "projectGuids";

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

        async Task<HttpStatusCode> Post(string id, string version, string ipAddress, string userAgent, string operation, string dependentPackage, string projectGuids, string uri,
            bool nonJSONRequest = false, bool nonPostRequest = false)
        {
            Console.WriteLine("Posting");
            var jObject = GetJObject(id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids);

            using (var client = new HttpClient())
            {
                Console.WriteLine("Using HttpClient");
                Console.WriteLine("Requesting response...");
                HttpResponseMessage response;
                if (nonPostRequest)
                {
                    response = await client.GetAsync(uri);
                }
                else
                {
                    response = await client.PostAsync(uri, new StringContent(nonJSONRequest ? "blah" : jObject.ToString(), Encoding.Default, "application/json"));
                }
                Console.WriteLine(response.StatusCode);
                Console.WriteLine("Received response");
                return response.StatusCode;
            }
        }

        async Task RunScenario(string scenario, HttpStatusCode expected, string id, string version, string ipAddress, string userAgent, string operation, string dependentPackage, string projectGuids, string uri,
            bool nonJSONRequest = false, bool nonPostRequest = false)
        {
            Console.WriteLine("Running scenario: " + scenario);
            Console.WriteLine("On " + uri);
            if (expected != await Post(id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, uri, nonJSONRequest: nonJSONRequest, nonPostRequest: nonPostRequest))
            {
                throw new InvalidOperationException("Response for " + scenario + " is not the expected status code" + expected);
            }
            Console.WriteLine("Successfully ran scenario: " + scenario);
        }

        async Task RunTestsForHost(string host)
        {
            var rootEndpoint = @"http://" + host + "/";
            var nonExistentEndpoint = @"http://" + host + "/DoesNotExist";
            var downloadEventEndpoint = @"http://" + host + "/DownloadEvent";

            var id = "DependentPackage";
            var version = "1.0.0"; // DependentPackage.1.0.0 should be present in the database the service writes to. Otherwise, the row cannot be added and the service logs the failure
            string ipAddress = null;
            string userAgent = "Console Test";
            string operation = "Install";
            string dependentPackage = null;
            string projectGuids = null;

            // Root endpoint
            var scenario = "Root endpoint";
            await RunScenario(scenario, HttpStatusCode.OK, id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, rootEndpoint);

            // Non existent endpoint
            scenario = "Non existent endpoint";
            await RunScenario(scenario, HttpStatusCode.NotFound, id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, nonExistentEndpoint);

            // Bad request. Request is not JSON
            scenario = "Bad request. Request is not JSON";
            await RunScenario(scenario, HttpStatusCode.BadRequest, id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, downloadEventEndpoint, nonJSONRequest: true);

            // Bad request. Request Method is not POST
            scenario = "Bad request: Request Method is not POST";
            await RunScenario(scenario, HttpStatusCode.BadRequest, id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, downloadEventEndpoint, nonJSONRequest: false, nonPostRequest: true);

            // Download Event endpoint
            scenario = "Download event endpoint";
            await RunScenario(scenario, HttpStatusCode.Accepted, id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, downloadEventEndpoint);
        }
    }
}
