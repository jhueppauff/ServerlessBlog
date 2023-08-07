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
using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ServerlessBlog.Frontend.API
{
    public class Metric
    {
        private readonly TableClient tableClient;

        public Metric()
        {
            tableClient = new TableClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), "metrics");
        }

        [Function(nameof(ProcessMetric))]
        public async Task<HttpResponseData> ProcessMetric(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/track")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Missing request body");
                return errorResponse;
            }

            Model.Metric body = JsonSerializer.Deserialize<Model.Metric>(requestBody);

            await tableClient.UpsertEntityAsync(new TableEntity()
            {
                PartitionKey = Convert.ToString(body.Slug),
                RowKey = Convert.ToString(body.SessionId),
                ["Timestamp"] = DateTime.UtcNow
            });

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            return response;
        }
    }
}