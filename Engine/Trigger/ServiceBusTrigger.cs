using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServerlessBlog.Engine.Services;
using ServerlessBlog.Engine.Constants;
using Microsoft.Azure.Functions.Worker;

namespace ServerlessBlog.Engine
{
    public class ServiceBusTrigger(ILoggerFactory loggerFactory, BlogMetadataService blogPostService, MarkdownBlobService markdownBlobService, HtmlBlobService htmlBlobService)
    {
        private readonly ILogger<ServiceBusTrigger> _logger = loggerFactory.CreateLogger<ServiceBusTrigger>();

        private readonly BlogMetadataService _blogPostService = blogPostService;
        private readonly MarkdownBlobService _markdownBlobService = markdownBlobService;
        private readonly HtmlBlobService _htmlBlobService = htmlBlobService;

        [Function(nameof(RenderPost))]
        public async Task RenderPost([ServiceBusTrigger(ServiceBusQueueNames.NewBlogPostQueue, Connection = "ServiceBusConnection")] QueueMessage message)
        {
            _logger.LogInformation($"ServicBus Trigger {nameof(RenderPost)} was triggered for {message.Slug}");
            string content = await _markdownBlobService.GetMarkdownAsync(message.Slug);
            await _htmlBlobService.UploadBlogHtmlContentAsync(content, message.Slug);
        }

        [Function(nameof(PublishPost))]
        public async Task PublishPost([ServiceBusTrigger(ServiceBusQueueNames.PublishBlogPostQueue, Connection = "ServiceBusConnection")] QueueMessage message)
        {
            _logger.LogInformation($"ServicBus Trigger {nameof(PublishPost)} was triggered for {message.Slug}");
            await _blogPostService.PublishBlogPostAsync(message.Slug);
        }
    }
}
