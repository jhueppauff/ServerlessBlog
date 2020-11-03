using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Azure.WebJobs.Extensions.HttpApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ServerlessBlog.Frontend
{
    public class StaticPageFunctions : HttpFunctionBase
    {
        public StaticPageFunctions(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        [FunctionName("IndexPage")]
        public IActionResult Index(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/Index")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            return File("/Statics/index.html");
        }

        [FunctionName("PostPage")]
        public async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/Post/{slug}")] HttpRequest req,
            [Blob("published/{slug}.html", FileAccess.Read, Connection = "AzureStorageConnection")] string postContent,
            [Table("metadata", "{slug}", "{slug}", Connection = "AzureStorageConnection")] PostMetadata postMetadata,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string content = await System.IO.File.ReadAllTextAsync("./Statics/post.html", System.Text.Encoding.UTF8).ConfigureAwait(false);
            content = content.Replace("$content$", postContent);
            content = content.Replace("$date$", postMetadata.Published);
            content = content.Replace("$titel$", postMetadata.Title);


            var result = new ContentResult
            {
                Content = content,
                ContentType = "text/html"
            };


            return result;
        }
    }
}
