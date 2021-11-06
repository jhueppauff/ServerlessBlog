using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.WebJobs.Extensions.HttpApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Globalization;

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
            [Table("metadata", Connection = "CosmosDBConnection")] CloudTable cloudTableClient,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string content = await System.IO.File.ReadAllTextAsync(Path.Combine(context.FunctionDirectory, "../statics/index.html"), System.Text.Encoding.UTF8).ConfigureAwait(false);

            TableQuery<PostMetadata> query = new TableQuery<PostMetadata>();

            StringBuilder indexContent = new StringBuilder();
            CultureInfo cultureInfo = new CultureInfo("en-us");

            foreach (var entity in (await cloudTableClient.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(false)).OrderByDescending(o => DateTime.Parse(o.Published, cultureInfo, DateTimeStyles.NoCurrentDateDefault)))
            {
                string tags = string.Empty;

                if (entity.Tags != null)
                {
                    foreach (string tag in entity.Tags.Split(';'))
                    {
                        tags += $"<li>{tag}</li>";
                    }
                }

                string html = @$"<div class='card mb-4 shadow-lg' style='background-color: #303030;'>
                                    <div class='card-body'>
                                        <div style='opacity: 0.8; height: 250px; width: 100%; background-size: cover; background-image: url({entity.ImageUrl}); background-repeat: no-repeat; heigth: 250px;'>
                                        </div>
                                        <h2 class='card-title'><a href='Post/{entity.PartitionKey}'>{entity.Title}</a></h2>
                                        <p class='card-text' style='color: white;'>{entity.Preview}</p>
                                        <div class='tags'>
                                            {tags}
                                        </div>
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

        [FunctionName(nameof(GetLicense))]
        public IActionResult GetLicense([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/license")] HttpRequest req)
        {
            return File("statics/license.html");
        }

        [FunctionName(nameof(PostPage))]
        public async Task<IActionResult> PostPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/Post/{slug}")] HttpRequest req,
            [Blob("published/{slug}.html", FileAccess.Read, Connection = "AzureStorageConnection")] string postContent,
            [Table("metadata", "{slug}", "{slug}", Connection = "CosmosDBConnection")] PostMetadata postMetadata,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string content = await System.IO.File.ReadAllTextAsync(Path.Combine(context.FunctionDirectory, "../statics/post.html"), System.Text.Encoding.UTF8).ConfigureAwait(false);
            content = content.Replace("$content$", postContent);
            content = content.Replace("$date$", postMetadata.Published);
            content = content.Replace("$titel$", postMetadata.Title);
            content = content.Replace("$description$", postMetadata.Preview);
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
