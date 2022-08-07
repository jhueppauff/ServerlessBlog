using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Engine.Model;
using System.Collections.Generic;

namespace Engine
{
    public class ImageProcessor
    {

        [FunctionName(nameof(UploadImage))]
        public static async Task UploadImage([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Post", Route = "Image/Upload/{extension}")] HttpRequest request, string extension,
        [Blob("public", FileAccess.Write, Connection = "AzureStorageConnection")] BlobContainerClient container, ILogger log)
        {
            log.LogInformation("Triggered Upload Function");

            BlobClient blobClient = container.GetBlobClient($"{Guid.NewGuid()}.{extension}");

            new FileExtensionContentTypeProvider().TryGetContentType($"{Guid.NewGuid()}.{extension}", out string contentType);

            using Stream stream = request.Form.Files[0].OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
        }

        [FunctionName(nameof(GetBlobs))]
        public static async Task<IActionResult> GetBlobs([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Get", Route = "Image")] HttpRequest request,
        [Blob("public", FileAccess.Write, Connection = "AzureStorageConnection")] BlobContainerClient container, ILogger log)
        {
            log.LogInformation("Triggered GetBlobs Function");
            List<Blob> blobs = new();

            var resultSegment = container.GetBlobsAsync()
            .AsPages(default, 50);

            await foreach (Azure.Page<BlobItem> blobPage in resultSegment)
            {
                foreach (BlobItem blob in blobPage.Values)
                {
                    blobs.Add(new Blob 
                    { 
                        Name = blob.Name, 
                        Url = $"{ container.Uri}/{blob.Name}"
                    });
                }
            }

            return new OkObjectResult(blobs);
        }

        public static async Task DeleteBlob([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Delete", Route = "Image/{blobName}")] HttpRequest request, string blobName,
        [Blob("public", FileAccess.Write, Connection = "AzureStorageConnection")] BlobContainerClient container, ILogger log)
        {
            await container.DeleteBlobIfExistsAsync(blobName);
        }
    }
}