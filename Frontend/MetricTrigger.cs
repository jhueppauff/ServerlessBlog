using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure.Data.Tables;
using ServerlessBlog.Frontend.Model;


namespace ServerlessBlog.Frontend
{
    public class MetricTrigger 
    {
        private readonly TableClient tableClient;

        public MetricTrigger()
        {
            this.tableClient = new TableClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metrics");
        }

        [Function(nameof(ProcessMetric))]
        public async Task<IActionResult> ProcessMetric(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/track")] HttpRequestData req)
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