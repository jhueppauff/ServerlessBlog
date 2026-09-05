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
using Azure.Storage.Blobs;
using System.Collections.Generic;
using Azure;
using ServerlessBlog.Frontend.Model;
using System.Net;

namespace ServerlessBlog.Frontend.HTTP
{
    public class PageProcessor(ILoggerFactory loggerFactory)
    {
        private readonly TableClient _tableClient = new(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metadata");
        private readonly BlobContainerClient _blobPublishedContainerClient = new(Environment.GetEnvironmentVariable("AzureStorageConnection"), "published");
        private readonly string _executionDirectory = Environment.CurrentDirectory;
        private readonly ILogger<PageProcessor> _logger = loggerFactory.CreateLogger<PageProcessor>();

        [Function(nameof(GetStaticContent))]
        public async Task<IActionResult> GetStaticContent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{filename:regex(^.+\\..+$)}")] HttpRequest req, string filename)
        {
            _logger.LogInformation("Get Static Content");
            string path = Path.Combine(_executionDirectory, $"statics/{filename}");

            if (!VerifyPathUnderRoot(path, _executionDirectory))
            {
            // Returning not found when directory is changed
            return new NotFoundResult();
            }

            if (!File.Exists(path))
            {
            return new NotFoundResult();
            }

            string content = await File.ReadAllTextAsync(path, Encoding.UTF8);
            IActionResult response;

            if (filename.EndsWith(".css"))
            {
            response = new ContentResult
            {
                Content = content,
                ContentType = "text/css; charset=utf-8",
                StatusCode = (int)HttpStatusCode.OK
            };
            }
            else if (filename.EndsWith(".js"))
            {
            response = new ContentResult
            {
                Content = content,
                ContentType = "text/javascript; charset=utf-8",
                StatusCode = (int)HttpStatusCode.OK
            };
            }
            else if (filename.EndsWith(".ico"))
            {
            response = new ContentResult
            {
                Content = content,
                ContentType = "image/x-icon; charset=utf-8",
                StatusCode = (int)HttpStatusCode.OK
            };
            }
            else
            {
            response = new BadRequestResult();
            }

            return response;
        }

        [Function(nameof(IndexPage))]
        public async Task<IActionResult> IndexPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{ignored:maxlength(0)?}")] HttpRequest req, string ignored = "")
        {
            _logger.LogInformation("Get Blog Home");

            var mainPage = await GetPostByPageUrlAsync("/");
            if (mainPage != null)
            {
                return await RenderPostPageAsync(mainPage.PartitionKey);
            }

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
                string pageUrl = GetPostPageUrl(post);
                stringBuilder.AppendLine($"<h2 class='card-title' style='margin-bottom: 0;'><a href='{pageUrl}'>{post.Title}</a></h2>");
                stringBuilder.AppendLine($"<p class='card-text' style='color: gray; margin-top: -4px;'>{publishDate.ToString("dd.MM.yyyy")}</p>");
                stringBuilder.AppendLine($"<p class='card-text' style='color: white;'>{post.Preview}</p>");
                stringBuilder.AppendLine("<div class='tags'>");
                stringBuilder.AppendLine(tags);
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine("</br>");
                stringBuilder.AppendLine($"<a href='{pageUrl}' class='btn btn-primary'>Read More &rarr;</a>");
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine("</div>");

                indexContent.AppendLine(stringBuilder.ToString());
            }

            content = content.Replace("$post$", indexContent.ToString());
            content = content.Replace("$appikey$", Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

            return new ContentResult
            {
                Content = content,
                ContentType = "text/html; charset=utf-8",
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        private async Task<List<PostMetadata>> GetPostsAsync()
        {
            AsyncPageable<TableEntity> queryResultsMaxPerPage = _tableClient.QueryAsync<TableEntity>(filter: $"IsPublic eq true", maxPerPage: 100);

            List<PostMetadata> postMetadata = [];

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
                        PageUrl = qEntity.GetString("PageUrl"),
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
        public async Task<IActionResult> GetLicense(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "license")] HttpRequest req)
        {
            string content = await File.ReadAllTextAsync(Path.Combine(_executionDirectory, "statics/license.html"), Encoding.UTF8);

            return new ContentResult
            {
            Content = content,
            ContentType = "text/html; charset=utf-8",
            StatusCode = (int)HttpStatusCode.OK
            };
        }

        [Function(nameof(PostPage))]
        public async Task<IActionResult> PostPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Post/{slug}")] HttpRequest req, string slug,
            [BlobInput("published/{slug}.html", Connection = "AzureStorageConnection")] string postContent)
        {
            _logger.LogInformation("Get Blob Post Page");
            return await RenderPostPageAsync(slug, postContent);
        }

        [Function(nameof(DynamicPage))]
        public async Task<IActionResult> DynamicPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*pagePath}")] HttpRequest req, string pagePath)
        {
            if (string.IsNullOrWhiteSpace(pagePath))
            {
                return new NotFoundResult();
            }

            var post = await GetPostByPageUrlAsync(ToAbsolutePath(pagePath));
            if (post == null)
            {
                return new NotFoundResult();
            }

            return await RenderPostPageAsync(post.PartitionKey);
        }

        private async Task<IActionResult> RenderPostPageAsync(string slug, string? postContent = null)
        {
            if (string.IsNullOrWhiteSpace(postContent))
            {
                var blob = _blobPublishedContainerClient.GetBlobClient($"{slug}.html");
                if (!await blob.ExistsAsync().ConfigureAwait(false))
                {
                    return new NotFoundResult();
                }

                postContent = (await blob.DownloadContentAsync().ConfigureAwait(false)).Value.Content.ToString();
            }

            try
            {
                var postMetadata = await _tableClient.GetEntityAsync<TableEntity>(slug, slug).ConfigureAwait(false);
                string content = await File.ReadAllTextAsync(Path.Combine(_executionDirectory, "statics/post.html"), Encoding.UTF8).ConfigureAwait(false);
                content = content.Replace("$content$", postContent);
                content = content.Replace("$date$", postMetadata.Value.GetString("Published"));
                content = content.Replace("$titel$", postMetadata.Value.GetString("Title"));
                content = content.Replace("$description$", postMetadata.Value.GetString("Preview"));
                content = content.Replace("$slug$", slug);
                content = content.Replace("$appikey$", Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

                return new ContentResult
                {
                    Content = content,
                    ContentType = "text/html; charset=utf-8",
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return new NotFoundResult();
            }
        }

        private async Task<PostMetadata?> GetPostByPageUrlAsync(string pageUrl)
        {
            if (string.IsNullOrWhiteSpace(pageUrl))
            {
                return null;
            }

            string escapedPageUrl = pageUrl.Replace("'", "''");
            var results = _tableClient.QueryAsync<TableEntity>(filter: $"IsPublic eq true and PageUrl eq '{escapedPageUrl}'", maxPerPage: 1);

            await foreach (Page<TableEntity> page in results.AsPages())
            {
                if (page.Values.Count == 0)
                {
                    break;
                }
                TableEntity entity = page.Values[0];

                return new PostMetadata()
                {
                    PartitionKey = entity.PartitionKey,
                    RowKey = entity.RowKey,
                    PageUrl = entity.GetString("PageUrl"),
                    IsPublic = entity.GetBoolean("IsPublic") ?? false
                };
            }

            return null;
        }

        private static string GetPostPageUrl(PostMetadata post)
        {
            if (!string.IsNullOrWhiteSpace(post.PageUrl))
            {
                return ToAbsolutePath(post.PageUrl);
            }

            return $"/Post/{post.PartitionKey}";
        }

        private static string ToAbsolutePath(string pagePath)
        {
            if (string.IsNullOrWhiteSpace(pagePath))
            {
                return "/";
            }

            string normalized = pagePath.Trim();
            normalized = normalized.Trim('/');

            return normalized.Length == 0 ? "/" : "/" + normalized;
        }

        private static bool VerifyPathUnderRoot(string pathToVerify, string rootPath = ".")
        {
            var fullRoot = Path.GetFullPath(rootPath);
            var fullPathToVerify = Path.GetFullPath(pathToVerify);
            return fullPathToVerify.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
