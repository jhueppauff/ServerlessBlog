using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ServerlessBlog.Engine.Constants;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ServerlessBlog.Engine.Services
{
    public class MarkdownBlobService
    {
        private readonly ILogger<MarkdownBlobService> logger;

        private readonly BlobContainerClient blobMarkdownContainerClient;

        public MarkdownBlobService(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
        {
            this.logger = loggerFactory.CreateLogger<MarkdownBlobService>();
            blobMarkdownContainerClient = blobServiceClient.GetBlobContainerClient(BlobContainerNames.MarkdownContainer);
        }

        public async Task<string> GetMarkdownAsync(string slug)
        {
            try
            {
                var blob = blobMarkdownContainerClient.GetBlobClient($"{slug}.md");
                BlobDownloadResult result = await blob.DownloadContentAsync();

                string markdownText = result.Content.ToString();

                logger.LogInformation($"Retrieved sucessfully markdown for {slug}");
                return markdownText;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occured while retrieving markdown for {slug}");
                throw;
            }
        }

        public async Task DeleteMarkdownAsync(string slug)
        {
            try
            {
                await blobMarkdownContainerClient.DeleteBlobIfExistsAsync($"{slug}.md");
                logger.LogInformation($"Deleted sucessfully markdown for {slug}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occured while deleting markdown for {slug}");
                throw;
            }
        }

        public async Task UploadMarkdownAsync(string content, string slug)
        {
            try
            {
                content = HttpUtility.HtmlEncode(content);
                var blob = blobMarkdownContainerClient.GetBlobClient($"{slug}.md");

                using (MemoryStream mstream = new(Encoding.UTF8.GetBytes(content)))
                {
                    await blob.UploadAsync(mstream, overwrite: true);
                }

                await blob.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders()
                {
                    ContentType = "text/markdown",
                    ContentEncoding = "utf-8"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occured while uploading markdown for {slug}");
                throw;
            }
        }
    }
}
