using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;

namespace AlertHawk.Monitoring.Infrastructure.Utils;

[ExcludeFromCodeCoverage]
public static class BlobUtils
{
    public static async Task<string?> UploadByteArrayToBlob(string blobName, byte[] data)
    {
        try
        {
            var containerName = Environment.GetEnvironmentVariable("azure_blob_storage_container_name");
            var connectionString = Environment.GetEnvironmentVariable("azure_blob_storage_connection_string");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            using MemoryStream stream = new MemoryStream(data);
            await blobClient.UploadAsync(stream, true);

            return blobClient.Uri.ToString();
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
            return null;
        }
    }
}