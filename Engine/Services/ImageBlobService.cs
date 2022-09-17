using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Engine.Constants;
using Engine.Model;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Engine.Services
{
    public class ImageBlobService
    {
        private readonly ILogger<ImageBlobService> logger;

        private readonly BlobContainerClient blobImageContainerClient;

        public ImageBlobService(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
        {
            this.logger = loggerFactory.CreateLogger<ImageBlobService>();
            blobImageContainerClient = blobServiceClient.GetBlobContainerClient(BlobContainerNames.ImageContainer);
        }

        public async Task<Uri> UploadImageAsync(string extension, Stream stream)
        {
            string filename = $"{Guid.NewGuid()}.{extension}";

            try
            {
                BlobClient blobClient = blobImageContainerClient.GetBlobClient($"{Guid.NewGuid()}.{extension}");
                new FileExtensionContentTypeProvider().TryGetContentType(filename, out string contentType);

                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

                logger.LogInformation($"Sucessfully uploaded Image: {filename} to Azure Storage");

                return blobClient.Uri;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occured while uploading image {filename}");
                throw;
            }
        }

        public async Task<List<Blob>> GetImagesAsync()
        {
            try
            {
                List<Blob> blobs = new();

                var resultSegment = blobImageContainerClient.GetBlobsAsync()
                .AsPages(default, 50);

                await foreach (Azure.Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blob in blobPage.Values)
                    {
                        blobs.Add(new Blob
                        {
                            Name = blob.Name,
                            Url = $"{blobImageContainerClient.Uri}/{blob.Name}"
                        });
                    }
                }

                return blobs;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured while retrieving the images");
                throw;
            }
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            try
            {
                await blobImageContainerClient.DeleteBlobIfExistsAsync(blobName, DeleteSnapshotsOption.IncludeSnapshots);
                logger.LogInformation($"Deleted Blob {blobName}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occured while deleting blob {blobName}");
                throw;
            }
        }
    }
}
