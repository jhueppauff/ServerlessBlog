using Azure.Storage.Blobs;
using ServerlessBlog.Engine.Constants;
using Markdig;
using Markdig.Prism;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ServerlessBlog.Engine.Services
{
    public class HtmlBlobService(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
    {
        private readonly ILogger<HtmlBlobService> _logger = loggerFactory.CreateLogger<HtmlBlobService>();

        private readonly BlobContainerClient _blobPublishedContainerClient = blobServiceClient.GetBlobContainerClient(BlobContainerNames.PublishContainer);

        /// <summary>
        /// Deletes a Blog from the Html storage
        /// </summary>
        /// <param name="slug">Slug of the blog post to delete</param>
        public async Task DeleteBlogHtmlContentAsync(string slug)
        {
            try
            {
                await _blobPublishedContainerClient.DeleteBlobIfExistsAsync($"{slug}.html");
                _logger.LogInformation($"Sucessfully deleted html content for {slug}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occured while deleting html content for {slug}");
                throw;
            }
        }

        /// <summary>
        /// Uploads the Blog Html Content to Blob Storage
        /// </summary>
        /// <param name="content">Html content</param>
        /// <param name="slug">Slug of the Blog</param>
        public async Task UploadBlogHtmlContentAsync(string content, string slug)
        {
            try
            {
                MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UsePipeTables().UseBootstrap().UsePrism().Build();
                string html = Markdown.ToHtml(content, pipeline);

                var blob = _blobPublishedContainerClient.GetBlobClient($"{slug}.html");

                using (MemoryStream mstream = new(Encoding.UTF8.GetBytes(html)))
                {
                    await blob.UploadAsync(mstream, overwrite: true);
                }

                await blob.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders()
                {
                    ContentType = "text/html",
                    ContentEncoding = "utf-8"
                });
                _logger.LogInformation($"Sucessfully uploaded html content for {slug}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occured while uploading html content for {slug}");
                throw;
            }
        }
    }
}
