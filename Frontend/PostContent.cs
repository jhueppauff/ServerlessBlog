using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using Azure.Data.Tables;
using System.Threading.Tasks;
using ServerlessBlog.Frontend.HTTP;

namespace ServerlessBlog.Frontend
{
    public class PostContent
    {
        private readonly TableClient tableClient;
        private readonly ILogger<PageProcessor> _logger;

        public PostContent(ILoggerFactory loggerFactory)
        {
            tableClient = new TableClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metadata");
            _logger = loggerFactory.CreateLogger<PageProcessor>();
        }
        
        [Function("GetPost")]
        public IActionResult GetPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetPost/{slug}")] HttpRequestData req,
            [BlobInput("published/{slug}.html", Connection = "AzureStorageConnection")] string content)
        {
            _logger.LogInformation("Get Post was triggered.");

            return new OkObjectResult(content);
        }

        [Function("GetPostMetadata")]
        public async Task<IActionResult> GetPostMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetPostMetadata/{slug}")] HttpRequestData req, string slug)
        {
            _logger.LogInformation("Get PostMetadata was triggered");

            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestObjectResult("slug cannot be empty");
            }

            var result = await tableClient.GetEntityAsync<TableEntity>(slug, slug);

            return new OkObjectResult(result.Value);
        }
    }
}
