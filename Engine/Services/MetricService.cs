using Azure;
using Azure.Data.Tables;
using ServerlessBlog.Engine.Constants;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace ServerlessBlog.Engine.Services
{
    public class MetricService(ILoggerFactory loggerFactory, TableServiceClient tableServiceClient)
    {
        private readonly ILogger<MetricService> _logger = loggerFactory.CreateLogger<MetricService>();
        private readonly TableClient _tableClient = tableServiceClient.GetTableClient(TableNames.MetricTableName);

        public async Task CleanOldMetricsAsync(int days)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            AsyncPageable<TableEntity> queryResultsMaxPerPage = _tableClient.QueryAsync<TableEntity>(filter: $"Timestamp ge datetime'{DateTime.UtcNow.AddDays(days)}'", maxPerPage: 500);

            List<Task> deletionTasks = new();
            int deletionItemCount = 0;

            await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
            {
                List<TableTransactionAction> deleteEntities = new();

                foreach (var item in page.Values)
                {
                    deleteEntities.Add(new TableTransactionAction(TableTransactionActionType.Delete, new TableEntity(item.PartitionKey, item.RowKey)));
                    deletionItemCount++;
                }

                deletionTasks.Add(_tableClient.SubmitTransactionAsync(deleteEntities));
            }

            await Task.WhenAll(deletionTasks);

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogInformation($"Removed {deletionItemCount} old metric entries from database in {elapsedMs}ms");
        }

        public async Task<List<PageView>> GetPageMetricAsync(string slug)
        {
            string queryDate = DateTime.UtcNow.AddDays(-31).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
            string query = $"PartitionKey eq '{slug}'"; //and Timestamp ge datetime'{queryDate}'
            AsyncPageable<TableEntity> queryResultsMaxPerPage = _tableClient.QueryAsync<TableEntity>(filter: query, maxPerPage: 500);

            List<PageView> response = new();

            await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
            {
                var grouped = page.Values.GroupBy(x => x.Timestamp.Value.Date);

                foreach (var item in grouped)
                {
                    if (response.Any(x => x.Timestamp == item.Key))
                    {
                        var entry = response.Find(x => x.Timestamp == item.Key);
                        entry.Views = +item.Count();
                    }
                    else
                    {
                        response.Add(new PageView()
                        {
                            Timestamp = item.Key,
                            Views = item.Count(),
                            Slug = slug
                        });
                    }
                }
            }

            return response;
        }

        public async Task<List<PageView>> GetPageViews()
        {
            AsyncPageable<TableEntity> queryResultsMaxPerPage = _tableClient.QueryAsync<TableEntity>(filter: $"", maxPerPage: 500);

            List<PageView> response = new();

            await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
            {
                var grouped = page.Values.GroupBy(x => x.PartitionKey);

                foreach (var item in grouped)
                {
                    if (response.Any(x => x.Slug == item.Key))
                    {
                        var entry = response.Find(x => x.Slug == item.Key);
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

            return response;
        }

        public async Task<PageView> GetPageViews(string slug)
        {
            AsyncPageable<TableEntity> queryResultsMaxPerPage = _tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{slug}'", maxPerPage: 500);

            int views = 0;

            await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
            {
                foreach (TableEntity qEntity in page.Values)
                {
                    views++;
                }
            }

            var response = new PageView()
            {
                Slug = slug,
                Views = views
            };

            return response;
        }
    }
}
