using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Tables;
using Microsoft.AspNetCore.Http;
using Engine;
using Newtonsoft.Json;
using System.Text;
using System.Web;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Data.Tables;
using Azure;

namespace ServerlessBlog.Engine
{
    public class PostOperations
    {
        private readonly TableClient tableClient;

        public PostOperations()
        {
            tableClient = new TableClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metadata");
        }
        
        [FunctionName(nameof(GetMarkdown))]
        public async Task<IActionResult> GetMarkdown(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post/{slug}/markdown")] HttpRequest req, string slug,
            [Blob("posts", FileAccess.Read, Connection = "AzureStorageConnection")] BlobContainerClient container)
        {
            if(String.IsNullOrEmpty(slug))
            {
                slug = req.Query["slug"];
            }

            var blob = container.GetBlobClient(slug + ".md");
            BlobDownloadResult result = await blob.DownloadContentAsync();

            string markdownText = result.Content.ToString();
            return new OkObjectResult(markdownText);
        }

        [FunctionName(nameof(Delete))]
        public async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "post/{slug}")] HttpRequest request, string slug,
            [Blob("posts/{slug}.md", FileAccess.ReadWrite, Connection = "AzureStorageConnection")] BlobClient postBlob,
            [Blob("published/{slug}.html", FileAccess.ReadWrite, Connection = "AzureStorageConnection")] BlobClient publishedBlob, ILogger logger)
        {
            logger.LogInformation($"Delete Post {slug}");

            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestObjectResult("slug cannot be empty");
            }

            await postBlob.DeleteIfExistsAsync().ConfigureAwait(false);
            await publishedBlob.DeleteIfExistsAsync().ConfigureAwait(false);

            await tableClient.DeleteEntityAsync(slug, slug, Azure.ETag.All);

            return new OkResult();
        }

        [FunctionName(nameof(GetPosts))]
        public async Task<IActionResult> GetPosts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post")] HttpRequest req)
        {
            // Get all entities from table storage
            AsyncPageable<TableEntity> queryResultsMaxPerPage = tableClient.QueryAsync<TableEntity>(filter: $"", maxPerPage: 100);

            List<PostMetadata> postMetadata = new();
            
            await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
            {
                foreach (TableEntity qEntity in page.Values)
                {
                    postMetadata.Add(new PostMetadata()
                    {
                        PartitionKey = qEntity.PartitionKey,
                        RowKey = qEntity.RowKey,
                        Slug = qEntity.GetString("Slug"),
                        Title = qEntity.GetString("Title"),
                        ImageUrl = qEntity.GetString("ImageUrl"),
                        Tags = qEntity.GetString("Tags"),
                        Published = qEntity.GetString("Published"),
                        Preview = qEntity.GetString("Preview")
                    });
                }
            }

            return new JsonResult(postMetadata);
        }

        [FunctionName(nameof(GetPost))]
        public async Task<IActionResult> GetPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post/{slug}")] HttpRequest req, string slug)
        {
            var result = await tableClient.GetEntityAsync<TableEntity>(slug, slug);

            return new JsonResult(result.Value);
        }

        [FunctionName(nameof(SavePostContent))]
        public async Task<IActionResult> SavePostContent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "post/{slug}")] HttpRequest req, string slug,
        [Queue("created", Connection = "AzureStorageConnection")] QueueClient queue,
        [Blob("posts", FileAccess.ReadWrite, Connection = "AzureStorageConnection")] BlobContainerClient container)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = req.Query["slug"];
            }

            var blob = container.GetBlobClient(slug + ".md");

            string content = string.Empty;

            using (Stream stream = req.Body)
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    content = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            content = HttpUtility.HtmlEncode(content);

            using (MemoryStream mstream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                await blob.UploadAsync(mstream);
            }

            await blob.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders()
            {
                ContentType = "text/markdown",
                ContentEncoding = "utf-8"
            });

            await queue.SendMessageAsync(slug);

            return new OkObjectResult(slug);
        }

        [FunctionName(nameof(SavePostMetadata))]
        public async Task<IActionResult> SavePostMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "post")] HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("missing request body");
            }

            PostMetadata metadata = JsonConvert.DeserializeObject<PostMetadata>(requestBody);
            metadata.Published = System.DateTime.Now.Date.ToShortDateString();

            await tableClient.AddEntityAsync<TableEntity>(new TableEntity()
            {
                PartitionKey = metadata.Slug,
                RowKey = metadata.Slug,
                ["Slug"] = metadata.Slug,
                ["Title"] = metadata.Title,
                ["ImageUrl"] = metadata.ImageUrl,
                ["Tags"] = metadata.Tags,
                ["Published"] = metadata.Published,
                ["Preview"] = metadata.Preview
            });

            return new OkResult();
        }
    }
}
