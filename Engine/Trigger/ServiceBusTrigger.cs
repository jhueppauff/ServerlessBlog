using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServerlessBlog.Engine.Services;
using ServerlessBlog.Engine.Constants;
using Microsoft.Azure.Functions.Worker;

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
