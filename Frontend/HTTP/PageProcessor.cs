using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Globalization;
using Azure.Data.Tables;
using System.Collections.Generic;
using Azure;
using ServerlessBlog.Frontend.Model;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace ServerlessBlog.Frontend.HTTP
{
    public class PageProcessor 
    {
        private readonly TableClient _tableClient;
        private readonly string _executionDirectory;
        private readonly ILogger<PageProcessor> _logger;

        public PageProcessor(ILoggerFactory loggerFactory)
        {
            _tableClient = new TableClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metadata");
            _logger = loggerFactory.CreateLogger<PageProcessor>();
            _executionDirectory = Environment.CurrentDirectory;
        }

        [Function(nameof(GetStaticContent))]
        public async Task<HttpResponseData> GetStaticContent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/{filename}")] HttpRequestData req, string filename, FunctionContext context)
        {
            _logger.LogInformation("Get Static Content");

            string content = await File.ReadAllTextAsync(Path.Combine(_executionDirectory, $"statics/{filename}"), Encoding.UTF8).ConfigureAwait(false);
            var response = req.CreateResponse(HttpStatusCode.OK);
            
            if (filename.EndsWith(".css"))
            {
                response.Headers.Add("Content-Type", "text/css; charset=utf-8");
                await response.WriteStringAsync(content);
            }

            if (filename.EndsWith(".js"))
            {
                response.Headers.Add("Content-Type", "text/javascript; charset=utf-8");
                await response.WriteStringAsync(content);
            }

            if (filename.EndsWith(".ico"))
            {
                response.Headers.Add("Content-Type", "image/x-icon; charset=utf-8");
                await response.WriteStringAsync(content);
            }

            return response;
        }

        [Function(nameof(IndexPage))]
        public async Task<HttpResponseData> IndexPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/")] HttpRequestData req)
        {
            _logger.LogInformation("Get Blog Home");

            string content = await File.ReadAllTextAsync(Path.Combine(_executionDirectory, "statics/index.html"), Encoding.UTF8).ConfigureAwait(false);

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

                DateTime publishDate = DateTime.Parse(post.Published, CultureInfo.InvariantCulture);

                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine("<div class='card mb-4 shadow-lg' style='background-color: #303030;'>");
                stringBuilder.AppendLine("<div class='card-body'>");
                stringBuilder.AppendLine($"<div style='opacity: 0.8; height: 250px; width: 100%; background-size: cover; background-image: url({post.ImageUrl}); background-repeat: no-repeat; heigth: 250px;'>");
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine($"<h2 class='card-title' style='margin-bottom: 0;'><a href='Post/{post.PartitionKey}'>{post.Title}</a></h2>");
                stringBuilder.AppendLine($"<p class='card-text' style='color: gray; margin-top: -4px;'>{publishDate.ToString("dd.MM.yyyy")}</p>");
                stringBuilder.AppendLine($"<p class='card-text' style='color: white;'>{post.Preview}</p>");
                stringBuilder.AppendLine("<div class='tags'>");
                stringBuilder.AppendLine(tags);
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine("</br>");
                stringBuilder.AppendLine($"<a href='Post/{post.PartitionKey}' class='btn btn-primary'>Read More &rarr;</a>");
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine("</div>");

                indexContent.AppendLine(stringBuilder.ToString());
            }

            content = content.Replace("$post$", indexContent.ToString());
            content = content.Replace("$appikey$", Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");

            await response.WriteStringAsync(content);

            return response;
        }

        private async Task<List<PostMetadata>> GetPostsAsync()
        {
            AsyncPageable<TableEntity> queryResultsMaxPerPage = _tableClient.QueryAsync<TableEntity>(filter: $"IsPublic eq true", maxPerPage: 100);

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

        [Function(nameof(GetLicense))]
        public async Task<HttpResponseData> GetLicense([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "license")] HttpRequestData req)
        {
            string content = await File.ReadAllTextAsync(Path.Combine(_executionDirectory, "statics/license.html"), Encoding.UTF8);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");

            await response.WriteStringAsync(content);

            return response;
        }

        [Function(nameof(PostPage))]
        public async Task<HttpResponseData> PostPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Post/{slug}")] HttpRequestData req, string slug,
            [BlobInput("published/{slug}.html", Connection = "AzureStorageConnection")] string postContent)
        {
            _logger.LogInformation("Get Blob Post Page");
            var postMetadata = await _tableClient.GetEntityAsync<TableEntity>(slug, slug);

            string content = await File.ReadAllTextAsync(Path.Combine(_executionDirectory, "statics/post.html"), Encoding.UTF8).ConfigureAwait(false);
            content = content.Replace("$content$", postContent);
            content = content.Replace("$date$", postMetadata.Value.GetString("Published"));
            content = content.Replace("$titel$", postMetadata.Value.GetString("Title"));
            content = content.Replace("$description$", postMetadata.Value.GetString("Preview"));
            content = content.Replace("$appikey$", Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");

            await response.WriteStringAsync(content);

            return response;
        }
    }
}
