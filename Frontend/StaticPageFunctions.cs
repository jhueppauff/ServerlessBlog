using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Azure.WebJobs.Extensions.HttpApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
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

        [FunctionName(nameof(IndexPage))]
        public async Task<IActionResult> IndexPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/Index")] HttpRequest req,
            [Table("metadata", Connection = "AzureStorageConnection")] CloudTable cloudTableClient,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string content = await System.IO.File.ReadAllTextAsync(Path.Combine(context.FunctionDirectory, "../statics/index.html"), System.Text.Encoding.UTF8).ConfigureAwait(false);

            TableQuery<PostMetadata> query = new TableQuery<PostMetadata>();

            StringBuilder indexContent = new StringBuilder();

            foreach (var entity in await cloudTableClient.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(false))
            {                
                string html = @$"<div class='card mb-4'>
                                    <div class='card-body'>
                                        <h2 class='card-title'><a href='Post/{entity.PartitionKey}'>{entity.Title}</a></h2>
                                        <p class='card-text'>{entity.Preview}</p>
                                        <a href='Post/{entity.PartitionKey}' class='btn btn-primary'>Read More &rarr;</a>
                                   </div>
                                </div>";

                indexContent.AppendLine(html);
            }

            content = content.Replace("$post$", indexContent.ToString());
            content = content.Replace("$appikey$", Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

            var result = new ContentResult
            {
                Content = content,
                ContentType = "text/html"
            };

            return result;
        }

        [FunctionName(nameof(PostPage))]
        public async Task<IActionResult> PostPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/Post/{slug}")] HttpRequest req,
            [Blob("published/{slug}.html", FileAccess.Read, Connection = "AzureStorageConnection")] string postContent,
            [Table("metadata", "{slug}", "{slug}", Connection = "AzureStorageConnection")] PostMetadata postMetadata, 
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string content = await System.IO.File.ReadAllTextAsync(Path.Combine(context.FunctionDirectory, "../statics/post.html"), System.Text.Encoding.UTF8).ConfigureAwait(false);
            content = content.Replace("$content$", postContent);
            content = content.Replace("$date$", postMetadata.Published);
            content = content.Replace("$titel$", postMetadata.Title);
            content = content.Replace("$appikey$", Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

            var result = new ContentResult
            {
                Content = content,
                ContentType = "text/html"
            };

            return result;
        }
    }
}