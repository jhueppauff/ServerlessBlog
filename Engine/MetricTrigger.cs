using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure.WebJobs.Extensions.HttpApi;
using Azure;
using Newtonsoft.Json.Linq;
using System.Linq;
using Engine.Model;

namespace Engine
{
    public class MetricTrigger : HttpFunctionBase
    {
        private readonly TableClient tableClient;

        public MetricTrigger(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.tableClient = new TableClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metrics");
        }

        [FunctionName(nameof(GetPageView))]
        public async Task<IActionResult> GetPageView(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric/{slug}")] HttpRequest request, string slug,
            ILogger log)
        {
            log.LogInformation($"Function {nameof(GetPageView)} was triggered");

            AsyncPageable<TableEntity> queryResultsMaxPerPage = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{slug}'", maxPerPage: 500);

            int views = 0;

            await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
            {
                foreach (TableEntity qEntity in page.Values)
                {
                    views++;
                }
            }

            var response = new JObject
            {
                { "Views", views },
                { "Slug", slug }
            };

            return new OkObjectResult(response);
        }

        [FunctionName(nameof(GetPageViews))]
        public async Task<IActionResult> GetPageViews(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric")] HttpRequest request,
            ILogger log)
        {
            log.LogInformation($"Function {nameof(GetPageViews)} was triggered");

            AsyncPageable<TableEntity> queryResultsMaxPerPage = tableClient.QueryAsync<TableEntity>(filter: $"", maxPerPage: 500);

            List<PageView> response = new();

            await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
            {
                var grouped = page.Values.GroupBy(x => x.PartitionKey);

                foreach (var item in grouped)
                {
                    if (response.Where(x => x.Slug == item.Key).Any())
                    {
                        var entry = response.FirstOrDefault(x => x.Slug == item.Key);
                        entry.Views = +item.Count();
                    }
                    else
                    {
                        response.Add(new PageView()
                        {
                            Slug = item.Key,
                            Views = item.Count()
                        });
                    }
                }
            }

            return new OkObjectResult(response);
        }
    }
}
