using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Messaging.ServiceBus;
using ServerlessBlog.Engine.Services;
using ServerlessBlog.Engine.Constants;
using ServerlessBlog.Engine.Security;
using Newtonsoft.Json;
using HttpMultipartParser;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace ServerlessBlog.Engine
{
    public class HttpTrigger(ILoggerFactory loggerFactory, ImageBlobService imageBlobService, MarkdownBlobService markdownBlobService,
        BlogMetadataService blogPostService, HtmlBlobService htmlBlobService, MetricService metricService, ServiceBusClient serviceBusClient)
    {
        private readonly ILogger<HttpTrigger> _logger = loggerFactory.CreateLogger<HttpTrigger>();
        private readonly ServiceBusClient _serviceBusClient = serviceBusClient;
        private readonly ImageBlobService _imageBlobService = imageBlobService;
        private readonly MarkdownBlobService _markdownBlobService = markdownBlobService;
        private readonly BlogMetadataService _blogMetadataService = blogPostService;
        private readonly HtmlBlobService _htmlBlobService = htmlBlobService;
        private readonly MetricService _metricService = metricService;

        #region ImageTrigger
        [Function(nameof(UploadImage))]
        [OpenApiOperation(operationId: nameof(UploadImage), tags: ["Image"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "extension", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **extension** parameter")]
        [OpenApiRequestBody("application/x-www-form-urlencoded", typeof(Stream), Required = true, Description = "Body containing the Image")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the blob Url")]
        public async Task<IActionResult> UploadImage([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Put", Route = "Image/Upload/{extension}")] HttpRequest request, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return new BadRequestResult();
            }

            _logger.LogInformation($"Function {nameof(UploadImage)} was triggered");

            var content = await MultipartFormDataParser.ParseAsync(request.Body);
            Uri blobUri = await _imageBlobService.UploadImageAsync(extension, content.Files[0].Data);

            return new OkObjectResult(blobUri);
        }
        
        [Function(nameof(GetImages))]
        [OpenApiOperation(operationId: nameof(GetImages), tags: ["Image"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the blob Url")]
        public async Task<IActionResult> GetImages(
            [HttpTrigger(AuthorizationLevel.Anonymous, methods: new[] { "GET" }, Route = "Image")] HttpRequest request)
        {
            _logger.LogInformation($"Function {nameof(GetImages)} was triggered");
            var blobs = await _imageBlobService.GetImagesAsync();

            return new OkObjectResult(blobs);
        }

        [Function(nameof(DeleteImage))]
        [OpenApiOperation(operationId: nameof(DeleteImage), tags: ["Image"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "blobName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **blobName** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DeleteImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, methods: new[] { "DELETE" }, Route = "Image/{blobName}")] HttpRequest request, string blobName)
        {
            _logger.LogInformation($"Function {nameof(DeleteImage)} was triggered for {blobName}");
            if (string.IsNullOrWhiteSpace(blobName))
            {
            _logger.LogWarning("Missing blob name in request");
            return new BadRequestResult();
            }

            _logger.LogInformation($"Function {nameof(DeleteImage)} was triggered");
            await _imageBlobService.DeleteBlobAsync(blobName);

            return new OkResult();
        }
        #endregion

        #region BlogPostTrigger
        [Function(nameof(GetMarkdown))]
        [OpenApiOperation(operationId: nameof(GetMarkdown), tags: ["BlogPosts"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the markdown")]
        public async Task<IActionResult> GetMarkdown([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post/{slug}/markdown")] HttpRequest request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetMarkdown)} was triggered for {slug}");
            if (String.IsNullOrEmpty(slug))
            {
                slug = System.Web.HttpUtility.ParseQueryString(request!.Query.ToString())["slug"];
            }

            if (string.IsNullOrEmpty(slug) || string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestResult();
            }

            string markdownText = await _markdownBlobService.GetMarkdownAsync(slug);

            return new ContentResult
            {
                Content = markdownText,
                ContentType = "text/markdown; charset=utf-8",
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        [Function(nameof(DeletePost))]
        [OpenApiOperation(operationId: nameof(DeletePost), tags: ["BlogPosts"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DeletePost([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "post/{slug}")] HttpRequest request, string slug)
        {
            _logger.LogInformation($"Function {nameof(DeletePost)} was triggered for {slug}");

            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestResult();
            }

            await _markdownBlobService.DeleteMarkdownAsync(slug);
            await _htmlBlobService.DeleteBlogHtmlContentAsync(slug);

            await _blogMetadataService.DeleteBlogPostAsync(slug);

            return new OkResult();
        }

        [Function(nameof(GetBlogPosts))]
        [OpenApiOperation(operationId: nameof(GetBlogPosts), tags: ["BlogPosts"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetBlogPosts([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post")] HttpRequest request)
        {
            _logger.LogInformation($"Function {nameof(GetBlogPosts)} was triggered");

            var metadata = await _blogMetadataService.GetBlogPostMetadataAsync();

            return new OkObjectResult(metadata);
        }

        [Function(nameof(GetBlogPost))]
        [OpenApiOperation(operationId: nameof(GetBlogPost), tags: ["BlogPosts"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetBlogPost([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post/{slug}")] HttpRequest request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetBlogPost)} was triggered");
            var metadata = await _blogMetadataService.GetBlogPostMetadataAsync(slug);

            return new OkObjectResult(metadata);
        }

        [Function(nameof(SavePostContent))]
        [OpenApiOperation(operationId: nameof(SavePostContent), tags: ["BlogPosts"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("text/plain", typeof(string), Required = true)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> SavePostContent([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "post/{slug}")] HttpRequest request, string slug)
        {
            _logger.LogInformation($"Function {nameof(SavePostContent)} was triggered");
            if (string.IsNullOrWhiteSpace(slug))
            {
            slug = request.Query["slug"];
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestResult();
            }

            string content = await new StreamReader(request.Body).ReadToEndAsync();

            content = Regex.Unescape(content);

            if (string.IsNullOrWhiteSpace(content))
            {
                return new BadRequestResult();
            }

            await _markdownBlobService.UploadMarkdownAsync(content, slug);

            string body = System.Text.Json.JsonSerializer.Serialize(new QueueMessage() { Slug = slug });
            var sender = _serviceBusClient.CreateSender(ServiceBusQueueNames.NewBlogPostQueue);

            await sender.SendMessageAsync(new ServiceBusMessage(body));

            return new OkObjectResult(slug);
        }

        [Function(nameof(SchedulePostPublish))]
        [OpenApiOperation(operationId: nameof(SchedulePostPublish), tags: ["BlogPosts"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("application/json", typeof(string), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> SchedulePostPublish(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "publish")] HttpRequest request)
        {
            _logger.LogInformation($"Function {nameof(SchedulePostPublish)} was triggered");
            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestResult();
            }

            PublishRequest? publishRequest = JsonConvert.DeserializeObject<PublishRequest>(requestBody);
            ArgumentNullException.ThrowIfNull(publishRequest);
            _logger.LogInformation($"Received {nameof(PublishRequest)} for {publishRequest!.Slug} at {publishRequest.PublishDate}");

            string body = System.Text.Json.JsonSerializer.Serialize(new QueueMessage() { Slug = publishRequest.Slug });
            ServiceBusMessage message = new(Encoding.UTF8.GetBytes(body))
            {
                ScheduledEnqueueTime = publishRequest.PublishDate
            };

            var sender = _serviceBusClient.CreateSender(ServiceBusQueueNames.PublishBlogPostQueue);
            await sender.SendMessageAsync(message);

            return new OkResult();
        }

        [Function(nameof(SavePostMetadata))]
        [OpenApiOperation(operationId: nameof(SavePostMetadata), tags: ["BlogPosts"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("application/json", typeof(string), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> SavePostMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "post")] HttpRequest request)
        {
            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestResult();
            }

            PostMetadata? metadata = JsonConvert.DeserializeObject<PostMetadata>(requestBody);
            ArgumentNullException.ThrowIfNull(metadata);
            await _blogMetadataService.SavePostMetadataAsync(metadata!);

            return new OkResult();
        }
        #endregion

        #region MetricTrigger
        [Function(nameof(GetPageViewHistory))]
        [OpenApiOperation(operationId: nameof(GetPageViewHistory), tags: ["Metric"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the page view history")]
        public async Task<IActionResult> GetPageViewHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric/{slug}/history")] HttpRequest request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetPageViewHistory)} was triggered");
            if (string.IsNullOrWhiteSpace(slug))
            {
            return new BadRequestResult();
            }

            var pageMetric = await _metricService.GetPageMetricAsync(slug);

            return new OkObjectResult(pageMetric);
        }

        [Function(nameof(GetPageViews))]
        [OpenApiOperation(operationId: nameof(GetPageViews), tags: ["Metric"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the page view history")]
        public async Task<IActionResult> GetPageViews([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric")] HttpRequest request)
        {
            _logger.LogInformation($"Function {nameof(GetPageViews)} was triggered");
            var pageViews = await _metricService.GetPageViews();

            return new OkObjectResult(pageViews);
        }

        [Function(nameof(GetPageView))]
        [OpenApiOperation(operationId: nameof(GetPageView), tags: ["Metric"])]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the page view history")]
        public async Task<IActionResult> GetPageView([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric/{slug}")] HttpRequest request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetPageView)} was triggered");

            var pageViews = await _metricService.GetPageViews(slug);

            return new OkObjectResult(pageViews);
        }
        #endregion
    }
}
