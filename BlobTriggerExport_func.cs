using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Foo.Function
{
    public class BlobTriggerExport_func
    {
        [FunctionName("BlobTriggerExport_func")]
        public async static Task Run([BlobTrigger("<ContainerName>/<DirectoryName>/{name}", Connection = "AzureWebJobsStorage")]BlobClient inputBlob,
        string name,
        ILogger log)
        {
            await CopyBlob(inputBlob, name, log);
        }
        /*
        ** Open an async read stream on Blob and upload that through AWS Upload Async stream method
        */
        public async static Task CopyBlob(BlobClient myBlob, string name, ILogger log)
        {
            var keyName = myBlob.Name;
            var existingBucketName = Environment.GetEnvironmentVariable("etlProdBucketName");
            var awsAccessKey = Environment.GetEnvironmentVariable("etlProdAwsAccessKey");
            var awsSecretKey = Environment.GetEnvironmentVariable("etlProdAwsSecretKey");

            TransferUtility fileTransferUtility = new TransferUtility(new AmazonS3Client(awsAccessKey,awsSecretKey,Amazon.RegionEndpoint.USEast1));

            log.LogInformation($"Starting Copy of {keyName}");

            try
            {
                using (var stream = await myBlob.OpenReadAsync())
                {
                    await fileTransferUtility.UploadAsync(stream,existingBucketName,keyName);
                }
                log.LogInformation($"Copy completed of ******{keyName}******");

            }
            catch(Exception ex)
            {
                log.LogError(ex.Message);
                log.LogInformation("Copy failed");
                // include email error to group
            }
            finally
            {
                log.LogInformation($"Operation completed of {keyName}");
            }
        }
    }
}
