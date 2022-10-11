using System;
using System.IO;
using System.Threading.Tasks;
using Azure.WebJobs.Extensions.HttpApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure.Data.Tables;
using ServerlessBlog.Frontend.Model;


namespace ServerlessBlog.Frontend
{
    public class MetricTrigger : HttpFunctionBase
    {
        private readonly TableClient tableClient;

        public MetricTrigger(IHttpContextAccessor httpContextAccessor): base(httpContextAccessor)
        {
            this.tableClient = new TableClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metrics");
        }

        [FunctionName(nameof(ProcessMetric))]
        public async Task<IActionResult> ProcessMetric(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/track")] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("missing request body");
            }

            Metric body = JsonSerializer.Deserialize<Metric>(requestBody);

            await tableClient.UpsertEntityAsync<TableEntity>(new TableEntity()
            {
                PartitionKey = Convert.ToString(body.Slug),
                RowKey = Convert.ToString(body.SessionId),
                ["Timestamp"] = DateTime.UtcNow
            });

            return new OkResult();
        }
    }
}