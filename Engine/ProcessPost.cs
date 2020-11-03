using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Engine
{
    public static class ProcessPost
    {
        [FunctionName("RenderPost")]
        public static async Task RenderPost([BlobTrigger("posts/{name}.md", Connection = "AzureStorageConnection")]string post, string name,
        [Blob("published", FileAccess.Write, Connection = "AzureStorageConnection")]CloudBlobContainer container, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {post.Length} Bytes");

            MarkdownSharp.Markdown markdown = new MarkdownSharp.Markdown();
            string html = markdown.Transform(post);

            var blobRef = container.GetBlockBlobReference(name + ".html");

            await blobRef.UploadTextAsync(html).ConfigureAwait(false);
            
            blobRef.Properties.ContentType = "text/html";
            await blobRef.SetPropertiesAsync().ConfigureAwait(false);
        }
    }
}
