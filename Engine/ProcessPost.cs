using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Markdig;

namespace Engine
{
    public static class ProcessPost
    {
        [FunctionName("RenderPost")]
        public static async Task RenderPost([QueueTrigger("created", Connection = "AzureStorageConnection")] string slug,
        [Blob("posts/{queueTrigger}.md", FileAccess.Read, Connection = "AzureStorageConnection")] string postContent,
        [Blob("published", FileAccess.Write, Connection = "AzureStorageConnection")]CloudBlobContainer container, ILogger log)
        {
            log.LogInformation($"Processed blob\n Name:{slug}");

            MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UsePipeTables().UseBootstrap().Build();
            string html = Markdown.ToHtml(postContent, pipeline);

            var blobRef = container.GetBlockBlobReference(slug + ".html");

            await blobRef.UploadTextAsync(html).ConfigureAwait(false);
            
            blobRef.Properties.ContentType = "text/html";
            await blobRef.SetPropertiesAsync().ConfigureAwait(false);
        }
    }
}
