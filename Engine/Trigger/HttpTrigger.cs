using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Engine.Services;
using Engine.Model;
using System.Text;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Engine.Constants;

namespace Engine.Trigger
{
    public class HttpTrigger
    {
        private readonly ILogger<HttpTrigger> _logger;

        private readonly ImageBlobService _imageBlobService;
        private readonly MarkdownBlobService _markdownBlobService;
        private readonly BlogMetadataService _blogMetadataService;
        private readonly HtmlBlobService _htmlBlobService;
        private readonly MetricService _metricService;

        public HttpTrigger(ILoggerFactory loggerFactory, ImageBlobService imageBlobService, MarkdownBlobService markdownBlobService, 
            BlogMetadataService blogPostService, HtmlBlobService htmlBlobService, MetricService metricService)
        {
            _logger = loggerFactory.CreateLogger<HttpTrigger>();
            _imageBlobService = imageBlobService;
            _markdownBlobService = markdownBlobService;
            _blogMetadataService = blogPostService;
            _htmlBlobService = htmlBlobService;
            _metricService = metricService;
        }

        #region ImageTrigger
        [FunctionName(nameof(UploadImage))]
        [OpenApiOperation(operationId: nameof(UploadImage), tags: new[] { "Image" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "extension", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **extension** parameter")]
        [OpenApiRequestBody("application/x-www-form-urlencoded", typeof(Stream), Required = true, Description = "Body containing the Image")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the blob Url")]
        public async Task<IActionResult> UploadImage([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Put", Route = "Image/Upload/{extension}")] HttpRequest request, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension) || string.IsNullOrEmpty(extension))
            {
                return new BadRequestObjectResult("Missing extension");
            }

            _logger.LogInformation($"Function {nameof(UploadImage)} was triggered");
            Uri blobUri = await _imageBlobService.UploadImageAsync(extension, request.Form.Files[0].OpenReadStream());

            return new OkObjectResult($"{blobUri}");
        }

        [FunctionName(nameof(GetImages))]
        [OpenApiOperation(operationId: nameof(GetImages), tags: new[] { "Image" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the blob Url")]
        public async Task<IActionResult> GetImages([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Get", Route = "Image")] HttpRequest request)
        {
            _logger.LogInformation($"Function {nameof(GetImages)} was triggered");
            var blobs = await _imageBlobService.GetImagesAsync();

            return new OkObjectResult(blobs);
        }

        [FunctionName(nameof(DeleteImage))]
        [OpenApiOperation(operationId: nameof(DeleteImage), tags: new[] { "Image" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "blobName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **blobName** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DeleteImage([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Delete", Route = "Image/{blobName}")] HttpRequest request, string blobName)
        {
            _logger.LogInformation($"Function {nameof(DeleteImage)} was triggered for {blobName}");
            if (string.IsNullOrWhiteSpace(blobName) || string.IsNullOrEmpty(blobName))
            {
                return new BadRequestObjectResult("Missing Blob Name");
            }

            _logger.LogInformation($"Function {nameof(DeleteImage)} was triggered");
            await _imageBlobService.DeleteBlobAsync(blobName);

            return new OkResult();
        }
        #endregion

        #region BlogPostTrigger
        [FunctionName(nameof(GetMarkdown))]
        [OpenApiOperation(operationId: nameof(GetMarkdown), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the markdown")]
        public async Task<IActionResult> GetMarkdown([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post/{slug}/markdown")] HttpRequest req, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetMarkdown)} was triggered for {slug}");
            if (String.IsNullOrEmpty(slug))
            {
                slug = req.Query["slug"];
            }

            if (string.IsNullOrEmpty(slug) || string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestObjectResult("Missing Slug");
            }

            string markdownText = await _markdownBlobService.GetMarkdownAsync(slug);
            return new OkObjectResult(markdownText);
        }

        [FunctionName(nameof(DeletePost))]
        [OpenApiOperation(operationId: nameof(DeletePost), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DeletePost([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "post/{slug}")] HttpRequest request, string slug)
        {
            _logger.LogInformation($"Function {nameof(DeletePost)} was triggered for {slug}");

            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestObjectResult("slug cannot be empty");
            }

            await _markdownBlobService.DeleteMarkdownAsync(slug);
            await _htmlBlobService.DeleteBlogHtmlContentAsync(slug);

            await _blogMetadataService.DeleteBlogPostAsync(slug);

            return new OkResult();
        }

        [FunctionName(nameof(GetBlogPosts))]
        [OpenApiOperation(operationId: nameof(GetBlogPosts), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetBlogPosts([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post")] HttpRequest req)
        {
            _logger.LogInformation($"Function {nameof(GetBlogPosts)} was triggered");

            var response = await _blogMetadataService.GetBlogPostMetadataAsync();
            return new OkObjectResult(response);
        }

        [FunctionName(nameof(GetBlogPost))]
        [OpenApiOperation(operationId: nameof(GetBlogPost), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetBlogPost([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post/{slug}")] HttpRequest req, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetBlogPost)} was triggered");
            var result = await _blogMetadataService.GetBlogPostMetadataAsync(slug);

            return new OkObjectResult(result);
        }

        [FunctionName(nameof(SavePostContent))]
        [OpenApiOperation(operationId: nameof(SavePostContent), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("text/plain", typeof(string), Required = true)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> SavePostContent([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "post/{slug}")] HttpRequest req, string slug,
        [ServiceBus(ServiceBusQueueNames.NewBlogPostQueue, Connection = "ServiceBusConnection")] IAsyncCollector<dynamic> outputServiceBus)
        {
            _logger.LogInformation($"Function {nameof(SavePostContent)} was triggered");
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = req.Query["slug"];
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                return new BadRequestObjectResult("slug cannot be empty");
            }

            string content = string.Empty;
            using (Stream stream = req.Body)
            {
                using StreamReader reader = new(stream, Encoding.UTF8);
                content = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(content))
            {
                return new BadRequestObjectResult("Body cannot be empty");
            }

            await _markdownBlobService.UploadMarkdownAsync(content, slug);

            string body = System.Text.Json.JsonSerializer.Serialize(new QueueMessage() { Slug = slug});

            await outputServiceBus.AddAsync(body);
            
            return new OkObjectResult(slug);
        }

        [FunctionName(nameof(SchedulePostPublish))]
        [OpenApiOperation(operationId: nameof(SavePostContent), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("application/json", typeof(string), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> SchedulePostPublish(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "publish")] HttpRequest req,
        [ServiceBus(ServiceBusQueueNames.PublishBlogPostQueue, Connection = "ServiceBusConnection")] IAsyncCollector<ServiceBusMessage> outputServiceBus)
        {
            _logger.LogInformation($"Function {nameof(SchedulePostPublish)} was triggered");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("missing request body");
            }

            PublishRequest publishRequest = JsonConvert.DeserializeObject<PublishRequest>(requestBody);
            _logger.LogInformation($"Received {nameof(PublishRequest)} for {publishRequest.Slug} at {publishRequest.PublishDate}");

            string body = System.Text.Json.JsonSerializer.Serialize(new QueueMessage() { Slug = publishRequest.Slug });
            ServiceBusMessage message = new(Encoding.UTF8.GetBytes(body))
            {
                ScheduledEnqueueTime = publishRequest.PublishDate
            };

            await outputServiceBus.AddAsync(message);

            return new OkResult();
        }

        [FunctionName(nameof(SavePostMetadata))]
        [OpenApiOperation(operationId: nameof(SavePostMetadata), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("application/json", typeof(string), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> SavePostMetadata([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "post")] HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("missing request body");
            }

            PostMetadata metadata = JsonConvert.DeserializeObject<PostMetadata>(requestBody);
            await _blogMetadataService.SavePostMetadataAsync(metadata);

            return new OkResult();
        }
        #endregion

        #region MetricTrigger
        [FunctionName(nameof(GetPageViewHistory))]
        [OpenApiOperation(operationId: nameof(GetPageViewHistory), tags: new[] { "Metric" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the page view history")]
        public async Task<IActionResult> GetPageViewHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric/{slug}/history")] HttpRequest request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetPageViewHistory)} was triggered");
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrEmpty(slug))
            {
                return new BadRequestObjectResult($"Missing {nameof(slug)}");
            }

            var response = await _metricService.GetPageMetricAsync(slug);
            return new OkObjectResult(response);
        }

        [FunctionName(nameof(GetPageViews))]
        [OpenApiOperation(operationId: nameof(GetPageViews), tags: new[] { "Metric" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the page view history")]
        public async Task<IActionResult> GetPageViews([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric")] HttpRequest request)
        {
            _logger.LogInformation($"Function {nameof(GetPageViews)} was triggered");
            var response = await _metricService.GetPageViews();
            return new OkObjectResult(response);
        }

        [FunctionName(nameof(GetPageView))]
        [OpenApiOperation(operationId: nameof(GetPageView), tags: new[] { "Metric" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the page view history")]
        public async Task<IActionResult> GetPageView(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric/{slug}")] HttpRequest request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetPageView)} was triggered");

            var response = await _metricService.GetPageViews(slug);
            return new OkObjectResult(response);
        }
        #endregion
    }
}

