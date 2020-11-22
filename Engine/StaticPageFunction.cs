using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.WebJobs.Extensions.HttpApi;
using System.IO;
using System.Threading.Tasks;
using System;

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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/GetEditPostPage/{slug}")] string slug, ExecutionContext context,
        [Blob("published/{slug}.html", FileAccess.Read, Connection = "AzureStorageConnection")] string postContent,
        [Table("metadata", "{slug}", "{slug}", Connection = "AzureStorageConnection")] PostMetadata postMetadata,
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

        private static bool IsEasyAuthEnabled => bool.TryParse(Environment.GetEnvironmentVariable("WEBSITE_AUTH_ENABLED"), out var result) && result;
    }
}
