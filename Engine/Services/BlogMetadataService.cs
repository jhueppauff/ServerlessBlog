using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ServerlessBlog.Engine.Constants;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus;
using System.Text;

namespace ServerlessBlog.Engine.Services
{
    public class BlogMetadataService
    {
        private readonly ILogger<BlogMetadataService> _logger;
        private readonly TableClient _tableClient;
        private readonly ServiceBusClient _serviceBusClient;

        public BlogMetadataService(ILoggerFactory loggerFactory, TableServiceClient tableServiceClient, ServiceBusClient serviceBusClient)
        {
            this._logger = loggerFactory.CreateLogger<BlogMetadataService>();
            this._tableClient = tableServiceClient.GetTableClient(TableNames.MetadataTableName);
            this._serviceBusClient = serviceBusClient;
        }

        public async Task ScheduleBlogPostPublishAsync(PublishRequest publishRequest)
        {
            string body = System.Text.Json.JsonSerializer.Serialize(new QueueMessage() { Slug = publishRequest.Slug });
            ServiceBusMessage message = new(Encoding.UTF8.GetBytes(body))
            {
                ScheduledEnqueueTime = publishRequest.PublishDate
            };

            var sender = _serviceBusClient.CreateSender(ServiceBusQueueNames.PublishBlogPostQueue);
            await sender.SendMessageAsync(message);
        }

        public async Task PublishBlogPostAsync(string slug)
        {
            try
            {
                await _tableClient.UpdateEntityAsync<TableEntity>(new TableEntity()
                {
                    PartitionKey = slug,
                    RowKey = slug,
                    ["IsPublic"] = true
                }, ETag.All, TableUpdateMode.Merge);

                _logger.LogInformation($"Published Post {slug} sucessfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task DeleteBlogPostAsync(string slug)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(slug, slug, ETag.All);
                _logger.LogInformation($"Deleted Post {slug} sucessfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"There was an error deleting blog metadata for {slug}");
                throw;
            }
        }

        public async Task<List<PostMetadata>> GetBlogPostMetadataAsync()
        {
            try
            {
                // Get all entities from table storage
                AsyncPageable<TableEntity> queryResultsMaxPerPage = _tableClient.QueryAsync<TableEntity>(filter: $"", maxPerPage: 100);

                List<PostMetadata> postMetadata = new();

                await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
                {
                    foreach (TableEntity qEntity in page.Values)
                    {
                        postMetadata.Add(new PostMetadata()
                        {
                            PartitionKey = qEntity.PartitionKey,
                            RowKey = qEntity.RowKey,
                            Slug = qEntity.GetString("Slug"),
                            Title = qEntity.GetString("Title"),
                            ImageUrl = qEntity.GetString("ImageUrl"),
                            Tags = qEntity.GetString("Tags"),
                            Published = qEntity.GetString("Published"),
                            Preview = qEntity.GetString("Preview"),
                            IsPublic = qEntity.GetBoolean("IsPublic") ?? false
                        });
                    }
                }
                _logger.LogInformation($"Retrieved blog metadata sucessfully");
                return postMetadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "There was an error retrieving blog metadata");
                throw;
            }
        }

        public async Task<PostMetadata> GetBlogPostMetadataAsync(string slug)
        {
            try
            {
                _logger.LogInformation($"Retrieved blog metadata sucessfully for {slug}");

                var response = await _tableClient.GetEntityAsync<TableEntity>(slug, slug);

                PostMetadata postMetadata = new()
                {
                    PartitionKey = response.Value.PartitionKey,
                    RowKey = response.Value.RowKey,
                    Slug = response.Value.GetString("Slug"),
                    Title = response.Value.GetString("Title"),
                    ImageUrl = response.Value.GetString("ImageUrl"),
                    Tags = response.Value.GetString("Tags"),
                    Published = response.Value.GetString("Published"),
                    Preview = response.Value.GetString("Preview"),
                    IsPublic = response.Value.GetBoolean("IsPublic") ?? false
                };

                return postMetadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"There was an error retrieving blog metadata for {slug}");
                throw;
            }
        }

        public async Task SavePostMetadataAsync(PostMetadata postMetadata)
        {
            try
            {
                postMetadata.Published = System.DateTime.Now.Date.ToShortDateString();

                await _tableClient.UpsertEntityAsync(new TableEntity()
                {
                    PartitionKey = postMetadata.Slug,
                    RowKey = postMetadata.Slug,
                    ["Slug"] = postMetadata.Slug,
                    ["Title"] = postMetadata.Title,
                    ["ImageUrl"] = postMetadata.ImageUrl,
                    ["Tags"] = postMetadata.Tags,
                    ["Published"] = postMetadata.Published,
                    ["Preview"] = postMetadata.Preview,
                    ["IsPublic"] = postMetadata.IsPublic
                });

                _logger.LogInformation($"Saved blog metadata sucessfully for {postMetadata.Slug}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"There was an error saving blog metadata for {postMetadata.Slug}");
                throw;
            }
        }
    }
}
