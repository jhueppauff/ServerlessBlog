using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Markdig;
using Markdig.Prism;
using Azure.Storage.Blobs;
using System.Text;

namespace Engine
{
    public static class ProcessPost
    {
        [FunctionName("RenderPost")]
        public static async Task RenderPost([QueueTrigger("created", Connection = "AzureStorageConnection")] string slug,
        [Blob("posts/{queueTrigger}.md", FileAccess.Read, Connection = "AzureStorageConnection")] string postContent,
        [Blob("published", FileAccess.Write, Connection = "AzureStorageConnection")] BlobContainerClient container, ILogger log)
        {
            log.LogInformation($"Processed blob\n Name:{slug}");

            MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UsePipeTables().UseBootstrap().UsePrism().Build();
            string html = Markdown.ToHtml(postContent, pipeline);

            var blob = container.GetBlobClient(slug + ".html");

            using (MemoryStream mstream = new(Encoding.UTF8.GetBytes(html)))
            {
                await blob.UploadAsync(mstream);
            }

            await blob.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders()
            {
                ContentType = "text/html",
                ContentEncoding = "utf-8"
            });
        }
    }
}
