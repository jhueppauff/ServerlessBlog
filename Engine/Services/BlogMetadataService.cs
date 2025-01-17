﻿using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using ServerlessBlog.Engine.Constants;

namespace ServerlessBlog.Engine.Services
{
    public class BlogMetadataService(ILoggerFactory loggerFactory, TableServiceClient tableServiceClient)
    {
        private readonly ILogger<BlogMetadataService> _logger = loggerFactory.CreateLogger<BlogMetadataService>();
        private readonly TableClient _tableClient = tableServiceClient.GetTableClient(TableNames.MetadataTableName);

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

        public async Task<TableEntity> GetBlogPostMetadataAsync(string slug)
        {
            try
            {
                _logger.LogInformation($"Retrieved blog metadata sucessfully for {slug}");
                return (await _tableClient.GetEntityAsync<TableEntity>(slug, slug)).Value;
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
