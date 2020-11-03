using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Table;

namespace ServerlessBlog.Frontend
{
    public static class PostContent
    {
        [FunctionName("GetPost")]
        public static IActionResult GetPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetPost/{slug}")] HttpRequest req,
            [Blob("published/{slug}.html", FileAccess.Read, Connection = "AzureStorageConnection")] string content,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkObjectResult(content);
        }

        [FunctionName("GetPostMetadata")]
        public static IActionResult GetPostMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetPostMetadata/{slug}")] HttpRequest req,
            [Blob("published/{slug}.html", FileAccess.Read, Connection = "AzureStorageConnection")] string content,
            [Table("metadata", "{slug}", "{slug}", Connection = "AzureStorageConnection")] PostMetadata data,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkObjectResult(data);
        }
    }
}
