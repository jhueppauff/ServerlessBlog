using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using Azure.Data.Tables;
using System.Threading.Tasks;

namespace ServerlessBlog.Frontend
{
    public class PostContent
    {
        private readonly TableClient tableClient;

        public PostContent()
        {
            tableClient = new TableClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metadata");
        }
        
        [FunctionName("GetPost")]
        public static IActionResult GetPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetPost/{slug}")] HttpRequest req,
            [Blob("published/{slug}.html", FileAccess.Read, Connection = "AzureStorageConnection")] string content,
            ILogger log)
        {
            log.LogInformation("Get Post was triggered.");

            return new OkObjectResult(content);
        }

        [FunctionName("GetPostMetadata")]
        public async Task<IActionResult> GetPostMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetPostMetadata/{slug}")] HttpRequest req, string slug,
            ILogger log)
        {
            log.LogInformation("Get PostMetadata was triggered");

            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestObjectResult("slug cannot be empty");
            }

            var result = await tableClient.GetEntityAsync<TableEntity>(slug, slug);

            return new OkObjectResult(result.Value);
        }
    }
}
