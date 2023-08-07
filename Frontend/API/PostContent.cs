using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using ServerlessBlog.Frontend.HTTP;
using System.Net;

namespace ServerlessBlog.Frontend.API
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
        public async Task<HttpResponseData> GetPost(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetPost/{slug}")] HttpRequestData req,
        [BlobInput("published/{slug}.html", Connection = "AzureStorageConnection")] string content)
        {
            _logger.LogInformation("Get Post was triggered.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/html");

            await response.WriteStringAsync(content);

            return response;
        }

        [Function("GetPostMetadata")]
        public async Task<HttpResponseData> GetPostMetadata(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetPostMetadata/{slug}")] HttpRequestData req, string slug)
        {
            _logger.LogInformation("Get PostMetadata was triggered");

            if (string.IsNullOrWhiteSpace(slug))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Slug cannot be empty");
                return errorResponse;
            }

            var result = await tableClient.GetEntityAsync<TableEntity>(slug, slug);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.Value);

            return response;
        }
    }
}
