An Azure Function (Blob Trigger) that transfers files asynchronously from Azure Blob Storage to AWS S3 bucket.

Microsoft Azure Function that uses:
  "Microsoft.NET.Sdk.Functions" : Version="4.0.1"
  "AWSSDK.S3" : Version="3.7.8.13"
  "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs" : Version="5.0.0"
  "Azure.Storage.Blobs" : Version="12.11.0"

This functions requires:
- S3 Bucket Name
- Region of S3 Bucket
- AWS Access Key
- AWS Secret Key
- (blob storage connection string and azure tenant to host the Azure Function... of course)
