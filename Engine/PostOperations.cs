using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage.Blob;
using Engine;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using System.Text;
using System.Web;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ServerlessBlog.Engine
{
    public static class PostOperations
    {
        [FunctionName(nameof(Get))]
        public static async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get/{slug}", Route = null)] HttpRequest req, string slug,
            [Blob("posts", FileAccess.Read, Connection = "AzureStorageConnection")] CloudBlobContainer container)
        {
            if(string.IsNullOrEmpty.IsNullOrEmpty(slug))
            {
                slug = req.Query["slug"];
            }

            var blobRef = container.GetBlockBlobReference(slug + ".md");
            string markdownText = await blobRef.DownloadTextAsync();
            return new OkObjectResult(markdownText);
        }

        [FunctionName(nameof(Delete))]
        public static async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Posts/{slug}")] HttpRequest request, string slug,
            [Blob("posts/{slug}.md", FileAccess.ReadWrite, Connection = "AzureStorageConnection")] CloudBlob postBlob,
            [Blob("published/{slug}.html", FileAccess.ReadWrite, Connection = "AzureStorageConnection")] CloudBlob publishedBlob,
            [Table("metadata", Connection = "CosmosDBConnection")] CloudTable cloudTable, ILogger logger)
        {
            logger.LogInformation($"Delete Post {slug}");

            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestObjectResult("slug cannot be empty");
            }

            await postBlob.DeleteIfExistsAsync().ConfigureAwait(false);
            await publishedBlob.DeleteIfExistsAsync().ConfigureAwait(false);

            var entity = new PostMetadata
            {
                PartitionKey = slug,
                RowKey = slug,
                ETag = "*"
            };

            TableOperation deleteOperation = TableOperation.Delete(entity);
            await cloudTable.ExecuteAsync(deleteOperation).ConfigureAwait(false);

            return new OkResult();
        }

        [FunctionName(nameof(List))]
        public static async Task<IActionResult> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table("metadata", Connection = "CosmosDBConnection")] CloudTable cloudTableClient)
        {
            TableQuery<PostMetadata> query = new TableQuery<PostMetadata>();
            List<PostMetadata> postMetadata = new List<PostMetadata>();

            foreach (var item in await cloudTableClient.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(false))
            {
                postMetadata.Add(item);
            }

            return new JsonResult(postMetadata);
        }

        [FunctionName(nameof(Save))]
        public static async Task<IActionResult> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        [Queue("created", Connection = "AzureStorageConnection")] CloudQueue queue,
        [Blob("posts", FileAccess.ReadWrite, Connection = "AzureStorageConnection")] CloudBlobContainer container)
        {
            string slug = req.Query["slug"];
            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestObjectResult("slug cannot be empty");
            }

            var blobRef = container.GetBlockBlobReference(slug + ".md");

            string content = string.Empty;

            using (Stream stream = req.Body)
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    content = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            content = HttpUtility.HtmlEncode(content);

            await blobRef.UploadTextAsync(content);
            blobRef.Properties.ContentType = "text/markdown";
            await blobRef.SetPropertiesAsync();

            await queue.AddMessageAsync(new CloudQueueMessage(slug));

            return new OkObjectResult(slug);
        }

        [FunctionName(nameof(SetPostMetadata))]
        public static async Task<IActionResult> SetPostMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Table("metadata", Connection = "CosmosDBConnection")] CloudTable client)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("missing request body");
            }

            PostMetadata metadata = JsonConvert.DeserializeObject<PostMetadata>(requestBody);
            metadata.Published = System.DateTime.Now.Date.ToShortDateString();

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(metadata);

            await client.ExecuteAsync(insertOrMergeOperation);

            return new OkResult();
        }
    }
}
