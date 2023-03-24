using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.WebJobs.Extensions.HttpApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Globalization;
using Azure.Data.Tables;
using System.Collections.Generic;
using Azure;
using ServerlessBlog.Frontend.Model;

namespace ServerlessBlog.Frontend
{
    public class StaticPageFunctions : HttpFunctionBase
    {
        private readonly TableClient tableClient;

        public StaticPageFunctions(IHttpContextAccessor httpContextAccessor): base(httpContextAccessor)
        {
            this.tableClient = new TableClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metadata");
        }

        [FunctionName(nameof(IndexPage))]
        public async Task<IActionResult> IndexPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/")] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("Get Blog Home");

            string content = await System.IO.File.ReadAllTextAsync(Path.Combine(context.FunctionDirectory, "../statics/index.html"), System.Text.Encoding.UTF8).ConfigureAwait(false);

            var posts = await GetPostsAsync();

            StringBuilder indexContent = new();
            CultureInfo cultureInfo = new("en-us");

            foreach (var post in posts.OrderByDescending(o => DateTime.Parse(o.Published, cultureInfo, DateTimeStyles.NoCurrentDateDefault)))
            {
                string tags = string.Empty;

                if (post.Tags != null)
                {
                    foreach (string tag in post.Tags.Split(';'))
                    {
                        tags += $"<li>{tag}</li>";
                    }
                }
                //DateTime publishDate = DateTime.ParseExact(post.Published, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                StringBuilder stringBuilder= new();
                stringBuilder.AppendLine("<div class='card mb-4 shadow-lg' style='background-color: #303030;'>");
                stringBuilder.AppendLine("<div class='card-body'>");
                stringBuilder.AppendLine($"<div style='opacity: 0.8; height: 250px; width: 100%; background-size: cover; background-image: url({post.ImageUrl}); background-repeat: no-repeat; heigth: 250px;'>");
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine($"<h2 class='card-title'><a href='Post/{post.PartitionKey}'>{post.Title}</a></h2>");
                stringBuilder.AppendLine($"<p class='card-text' style='color: white;'>{post.Preview}</p>");
                stringBuilder.AppendLine("<div class='tags'>");
                stringBuilder.AppendLine(tags);
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine("</br>");
                stringBuilder.AppendLine("<div style='display: flex; justify-content: space-between; align-items: center;'>");
                stringBuilder.AppendLine($"<a href='Post/{post.PartitionKey}' class='btn btn-primary'>Read More &rarr;</a>");
                stringBuilder.AppendLine($"<p class='card-text' style='color: white'>{post.Published}</p>");
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine("</div>");

                indexContent.AppendLine(stringBuilder.ToString());
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

        private async Task<List<PostMetadata>> GetPostsAsync()
        {
            AsyncPageable<TableEntity> queryResultsMaxPerPage = tableClient.QueryAsync<TableEntity>(filter: $"IsPublic eq true", maxPerPage: 100);

            List<PostMetadata> postMetadata = new();

            await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
            {
                foreach (TableEntity qEntity in page.Values)
                {
                    postMetadata.Add(new PostMetadata()
                    {
                        PartitionKey = qEntity.PartitionKey,
                        RowKey = qEntity.RowKey,
                        Title = qEntity.GetString("Title"),
                        ImageUrl = qEntity.GetString("ImageUrl"),
                        Tags = qEntity.GetString("Tags"),
                        Published = qEntity.GetString("Published"),
                        Preview = qEntity.GetString("Preview"),
                        IsPublic = qEntity.GetBoolean("IsPublic") ?? false
                    });
                }
            }

            return postMetadata;
        }

        [FunctionName(nameof(GetLicense))]
        public IActionResult GetLicense([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "license")] HttpRequest req)
        {
            return File("statics/license.html");
        }

        [FunctionName(nameof(PostPage))]
        public async Task<IActionResult> PostPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Post/{slug}")] HttpRequest req, string slug,
            [Blob("published/{slug}.html", FileAccess.Read, Connection = "AzureStorageConnection")] string postContent,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("Get Blob Post Page");
            var postMetadata = await tableClient.GetEntityAsync<TableEntity>(slug, slug);

            string content = await System.IO.File.ReadAllTextAsync(Path.Combine(context.FunctionDirectory, "../statics/post.html"), System.Text.Encoding.UTF8).ConfigureAwait(false);
            content = content.Replace("$content$", postContent);
            content = content.Replace("$date$", postMetadata.Value.GetString("Published"));
            content = content.Replace("$titel$", postMetadata.Value.GetString("Title"));
            content = content.Replace("$description$", postMetadata.Value.GetString("Preview"));
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
