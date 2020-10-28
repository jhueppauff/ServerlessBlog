using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage.Blob;

namespace ServerlessBlog.Engine
{
    public static class GetBlogPost
    {
        [FunctionName(nameof(Get))]
        public static async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Blob("posts", FileAccess.Read, Connection = "AzureStorageConnection")] CloudBlobContainer container)
        {
            string slug = req.Query["slug"];
            var blobRef = container.GetBlockBlobReference(slug + ".md");
            string markdownText = await blobRef.DownloadTextAsync();
            return new OkObjectResult(markdownText);
        }

        [FunctionName(nameof(List))]
        public static IActionResult List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Blob("posts/index.json", FileAccess.ReadWrite, Connection = "AzureStorageConnection")] string index)
        {
            return new JsonResult(index);
        }

        [FunctionName(nameof(Save))]
        public static async Task<IActionResult> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        [Blob("posts", FileAccess.ReadWrite, Connection = "AzureStorageConnection")] CloudBlobContainer container)
        {
            string slug = req.Query["slug"];
            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestObjectResult("slug cannot be empty");
            }

            var blobRef = container.GetBlockBlobReference(slug + ".md");

            await blobRef.UploadFromStreamAsync(req.Body);
            blobRef.Properties.ContentType = "text/markdown";
            await blobRef.SetPropertiesAsync();

            return new OkObjectResult(slug);
        }
    }
}
