using System;
using Azure.Storage.Blobs;
using Markdig;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure;
using ServerlessBlog.Engine.Services;
using ServerlessBlog.Engine.Model;
using ServerlessBlog.Engine.Constants;

namespace ServerlessBlog.Engine
{
    public class ServiceBusTrigger
    {
        private readonly ILogger<ServiceBusTrigger> _logger;

        private readonly BlogMetadataService _blogPostService;
        private readonly MarkdownBlobService _markdownBlobService;
        private readonly HtmlBlobService _htmlBlobService;

        public ServiceBusTrigger(ILoggerFactory loggerFactory, BlogMetadataService blogPostService, MarkdownBlobService markdownBlobService, HtmlBlobService htmlBlobService)
        {
            this._logger = loggerFactory.CreateLogger<ServiceBusTrigger>();
            _blogPostService = blogPostService;
            _markdownBlobService = markdownBlobService;
            _htmlBlobService = htmlBlobService;
        }

        [FunctionName(nameof(RenderPost))]
        public async Task RenderPost([ServiceBusTrigger(ServiceBusQueueNames.NewBlogPostQueue, Connection = "ServiceBusConnection")] QueueMessage message)
        {
            _logger.LogInformation($"ServicBus Trigger {nameof(RenderPost)} was triggered for {message.Slug}");
            string content = await _markdownBlobService.GetMarkdownAsync(message.Slug);
            await _htmlBlobService.UploadBlogHtmlContentAsync(content, message.Slug);
        }

        [FunctionName(nameof(PublishPost))]
        public async Task PublishPost([ServiceBusTrigger(ServiceBusQueueNames.PublishBlogPostQueue, Connection = "ServiceBusConnection")] QueueMessage message)
        {
            _logger.LogInformation($"ServicBus Trigger {nameof(PublishPost)} was triggered for {message.Slug}");
            await _blogPostService.PublishBlogPostAsync(message.Slug);
        }
    }
}
