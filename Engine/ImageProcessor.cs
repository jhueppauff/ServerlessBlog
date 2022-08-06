using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Engine
{
    public class ImageProcessor
    {

        [FunctionName(nameof(UploadImage))]
        public static async Task UploadImage([HttpTrigger(AuthorizationLevel.Anonymous, Route = "Image/Upload/{extension}")] HttpRequest request, string extension,
        [Blob("public", FileAccess.Write, Connection = "AzureStorageConnection")] BlobContainerClient container, ILogger log)
        {
            log.LogInformation("Triggered Upload Function");

            BlobClient blobClient = container.GetBlobClient($"{Guid.NewGuid()}.{extension}");

            new FileExtensionContentTypeProvider().TryGetContentType($"{Guid.NewGuid()}.{extension}", out string contentType);

            using Stream stream = request.Form.Files[0].OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
        }
    }
}