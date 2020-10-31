using System;

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
        public IActionResult Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "static/Post")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            return File("/Statics/post.html");
        }
    }
}
