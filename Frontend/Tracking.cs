using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using ServerlessBlog.Frontend.Model;

namespace ServerlessBlog.Frontend
{
    public static class Tracking 
    {
        [FunctionName(nameof(TackPageView))]
        public static IActionResult TackPageView([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "track/{slug}")] HttpRequest req, string slug,
        [Table("metrics", Connection = "CosmosDBConnection")] out PageView document)
        {
            document = new PageView(slug)
            {
            };

            return new OkResult();
        }
    }
}