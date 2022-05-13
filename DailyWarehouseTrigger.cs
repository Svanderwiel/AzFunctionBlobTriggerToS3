using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure.Storage.Blobs;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace BYR.Function
{
    public class DailyTriggerFunction
    {
        [FunctionName("DailyTriggerFunction")]
        public void Run([TimerTrigger("0 15 3 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            // DMFManager dmfm = new DMFManager();
            // DMFManager.Export("TestExportToBlob", "Package_Name", string.Empty, log);
        }
    }
    public class DMFExport
    {
        [JsonProperty("definitionGroupId")]
        public string DefinitionGroupId { get; set; }

        [JsonProperty("packageName")]
        public string PackageName { get; set; }

        [JsonProperty("executionId")]
        public string ExecutionId { get; set; }

        [JsonProperty("reExecute")]
        public bool ReExecute { get; set; }

       [JsonProperty("legalEntityId")]
        public string LegalEntityId { get; set; }
    }
    /* ExecutionID */
    public class DMFExportSummary
    {      
        [JsonProperty("executionId")]
        public string ExecutionId { get; set; }     
    }
    public class DMFManager
    {
        //d365BlobUrl - The URL of temp blob that holds D365 export after DMF job run
        static string d365BlobUrl = string.Empty;

        static string primaryBlobSASUrl = System.Environment.GetEnvironmentVariable("BlobSASUrl");

        //Azure AAD Application settings
        //The Tenant URL (use friendlyname or the TenantID
        static string aadTenant = System.Environment.GetEnvironmentVariable("ActiveDirectoryTenantUrl");
        //The URL of the resource you would be accessing using the access token
        //Please ensure / is not there in the end of the URL
        static string aadResource = System.Environment.GetEnvironmentVariable("ActiveDirectoryResource");
        //APplication ID . Store them securely / Encrypted config file or secure store
        static string aadClientAppId = System.Environment.GetEnvironmentVariable("ActiveDirectoryClientId");
        //Application secret . Store them securely / Encrypted config file or secure store
        static string aadClientAppSecret = System.Environment.GetEnvironmentVariable("ActiveDirectorySecretId");     

        /// <summary>
        /// Retrieves an authentication header from the service.
        /// </summary>
        /// <returns>The authentication header for the Web API call.</returns>
        private static string GetAuthenticationHeader()
        {
            //using Microsoft.IdentityModel.Clients.ActiveDirectory;
            AuthenticationContext authenticationContext = new AuthenticationContext(aadTenant);
            var credential = new ClientCredential(aadClientAppId, aadClientAppSecret);
            AuthenticationResult authenticationResult = authenticationContext.AcquireTokenAsync(aadResource, credential).Result;
            return authenticationResult.AccessToken;
        }

        /// <summary>
        /// Configures PUT request Headers for BlobURL to Blob storage
        /// </summary>
        private static void PutBlobSASUrl(string d365BlobUrl, ILogger log)
        {
            string requestUri = System.Environment.GetEnvironmentVariable("BlobSASUrl");
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(requestUri);

            string now = DateTime.UtcNow.ToString("R");
            
            int contentLength = 0;

            client.DefaultRequestHeaders.Add("x-ms-date", now);
            client.DefaultRequestHeaders.Add("x-ms-version", "2020-04-08"); // https://docs.microsoft.com/en-us/rest/api/storageservices/versioning-for-the-azure-storage-services
            client.DefaultRequestHeaders.Add("Content-Length", contentLength.ToString());
            client.DefaultRequestHeaders.Add("x-ms-blob-type", "BlockBlob");
            client.DefaultRequestHeaders.Add("x-ms-copy-source", d365BlobUrl);

            log.LogInformation("Downloading Blob URL to Blob Storage");
        }

        // Setup Step 
        // - Create an export project within Dynamics called ExportVendors in company USMF before you run the following code
        // - It can of any data format XML and can include any number of data entities
        // 1. Initiate export of a data project to create a data package within Dynamics 365 for Operations
        // sv - removed string filePath, string fileName from param
        public static async void Export(string jobName, string packageName, string legalEntity, ILogger log)
        {
            string authHeader = GetAuthenticationHeader();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(aadResource);
            client.DefaultRequestHeaders.Clear();        
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authHeader);
            string executionID = System.Environment.GetEnvironmentVariable("ExecutionID");
            string outPut = string.Empty;
            int maxLoop = 6;

            do
            {
                log.LogInformation("Waiting for package to execution to complete...");

                Thread.Sleep(5000);
                maxLoop--;

                if (maxLoop <= 0)
                {
                    break;
                }
                log.LogInformation("Checking status...");

                var stringPayload = JsonConvert.SerializeObject(new DMFExportSummary() { ExecutionId = executionID });
                var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");

                var result = client.PostAsync("/data/DataManagementDefinitionGroups/Microsoft.Dynamics.DataEntities.GetExecutionSummaryStatus", httpContent).Result;
                string resultContent = await result.Content.ReadAsStringAsync();
                outPut = JObject.Parse(resultContent).GetValue("value").ToString();
            
                log.LogInformation("Status of export is "+ outPut);

            }
            while (outPut == "NotRun" || outPut == "Executing");

            if (outPut != "Succeeded" && outPut != "PartiallySucceeded")
            {
                throw new Exception("Operation Failed");
            }
            else
            {
                // 3. Get downloable Url to download the package    
                //    POST / data / DataManagementDefinitionGroups / Microsoft.Dynamics.DataEntities.GetExportedPackageUrl
                // stringPayload = JsonConvert.SerializeObject(new DMFExportSummary() { ExecutionId = executionID });
                // httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");

                // result = client.PostAsync("/data/DataManagementDefinitionGroups/Microsoft.Dynamics.DataEntities.GetExportedPackageUrl", httpContent).Result;
                // resultContent = await result.Content.ReadAsStringAsync();
                // log.LogInformation("resultContent");
                // d365BlobUrl = JObject.Parse(resultContent).GetValue("value").ToString();
            }

                // 4. Download the file from Url to a local folder
                // might be unnecessary for downloading url to blob rather than local file system

                // log.LogInformation("Downloading the file ...");
                // var blob = new BlobClient(new Uri(d365BlobUrl));
                // blob.DownloadTo(Path.Combine(filePath, fileName + ".zip"));
                // log.LogInformation("Downloading the file ...Complete");
        }  
    }     
}
