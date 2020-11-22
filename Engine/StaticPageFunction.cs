using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.WebJobs.Extensions.HttpApi;
using System.IO;
using System.Threading.Tasks;
using System;
using HtmlAgilityPack;

namespace Engine
{
    public class StaticPageFunction : HttpFunctionBase
    {
        public StaticPageFunction(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        [FunctionName(nameof(GetAddPostPage))]
        public async Task<IActionResult> GetAddPostPage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/GetAddPostPage")] HttpRequest req, ExecutionContext context,
            ILogger log)
        {
            if (!IsEasyAuthEnabled || !User.Identity.IsAuthenticated)
            {
                return Forbid();
            }

            string content = await System.IO.File.ReadAllTextAsync(Path.Combine(context.FunctionDirectory, "../statics/add-page.html"), System.Text.Encoding.UTF8).ConfigureAwait(false);
            content = content.Replace("$appikey$", Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

            var result = new ContentResult
            {
                Content = content,
                ContentType = "text/html"
            };

            return result;
        }

        [FunctionName(nameof(GetEditPostPage))]
        public async Task<IActionResult> GetEditPostPage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/GetEditPostPage/{slug}")] HttpRequest req, ExecutionContext context,
        [Blob("posts/{slug}.md", FileAccess.Read, Connection = "AzureStorageConnection")] string postContent,
        [Table("metadata", "{slug}", "{slug}", Connection = "AzureStorageConnection")] PostMetadata postMetadata,
        ILogger log)
        {
            if (!IsEasyAuthEnabled || !User.Identity.IsAuthenticated)
            {
                return Forbid();
            }

            string content = await System.IO.File.ReadAllTextAsync(Path.Combine(context.FunctionDirectory, "../statics/add-page.html"), System.Text.Encoding.UTF8).ConfigureAwait(false);
            content = content.Replace("$appikey$", Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(content);

            HtmlNode textArea = document.DocumentNode.SelectSingleNode("//*[@id=\"postcontent\"]");
            textArea.InnerHtml = postContent;

            HtmlNode title = document.DocumentNode.SelectSingleNode("//*[@id=\"title\"]");
            title.SetAttributeValue("value", postMetadata.Title);

            HtmlNode preview = document.DocumentNode.SelectSingleNode("//*[@id=\"preview\"]");
            preview.SetAttributeValue("value", postMetadata.Preview);

            var result = new ContentResult
            {
                Content = document.DocumentNode.OuterHtml,
                ContentType = "text/html"
            };

            return result;
        }

        private static bool IsEasyAuthEnabled => bool.TryParse(Environment.GetEnvironmentVariable("WEBSITE_AUTH_ENABLED"), out var result) && result;
    }
}
