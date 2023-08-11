using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Messaging.ServiceBus;
using ServerlessBlog.Engine.Services;
using ServerlessBlog.Engine.Constants;
using ServerlessBlog.Engine.Security;
using Newtonsoft.Json;
using HttpMultipartParser;
using System.Text.RegularExpressions;

namespace ServerlessBlog.Engine
{
    public class HttpTrigger
    {
        private readonly ILogger<HttpTrigger> _logger;
        private readonly ServiceBusClient _serviceBusClient;

        private readonly ImageBlobService _imageBlobService;
        private readonly MarkdownBlobService _markdownBlobService;
        private readonly BlogMetadataService _blogMetadataService;
        private readonly HtmlBlobService _htmlBlobService;
        private readonly MetricService _metricService;

        public HttpTrigger(ILoggerFactory loggerFactory, ImageBlobService imageBlobService, MarkdownBlobService markdownBlobService,
            BlogMetadataService blogPostService, HtmlBlobService htmlBlobService, MetricService metricService, ServiceBusClient serviceBusClient)
        {
            _logger = loggerFactory.CreateLogger<HttpTrigger>();
            _imageBlobService = imageBlobService;
            _markdownBlobService = markdownBlobService;
            _blogMetadataService = blogPostService;
            _htmlBlobService = htmlBlobService;
            _metricService = metricService;
            _serviceBusClient = serviceBusClient;
        }

        #region ImageTrigger
        [Function(nameof(UploadImage))]
        [OpenApiOperation(operationId: nameof(UploadImage), tags: new[] { "Image" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "extension", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **extension** parameter")]
        [OpenApiRequestBody("application/x-www-form-urlencoded", typeof(Stream), Required = true, Description = "Body containing the Image")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the blob Url")]
        public async Task<HttpResponseData> UploadImage([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Put", Route = "Image/Upload/{extension}")] HttpRequestData request, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            _logger.LogInformation($"Function {nameof(UploadImage)} was triggered");

            var content = await MultipartFormDataParser.ParseAsync(request.Body);
            Uri blobUri = await _imageBlobService.UploadImageAsync(extension, content.Files[0].Data);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(blobUri);

            return response;
        }

        [Function(nameof(GetImages))]
        [OpenApiOperation(operationId: nameof(GetImages), tags: new[] { "Image" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow),Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the blob Url")]
        public async Task<HttpResponseData> GetImages([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Get", Route = "Image")] HttpRequestData request)
        {
            _logger.LogInformation($"Function {nameof(GetImages)} was triggered");
            var blobs = await _imageBlobService.GetImagesAsync();

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(blobs);
            return response;
        }

        [Function(nameof(DeleteImage))]
        [OpenApiOperation(operationId: nameof(DeleteImage), tags: new[] { "Image" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "blobName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **blobName** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<HttpResponseData> DeleteImage([HttpTrigger(AuthorizationLevel.Anonymous, methods: "Delete", Route = "Image/{blobName}")] HttpRequestData request, string blobName)
        {
            _logger.LogInformation($"Function {nameof(DeleteImage)} was triggered for {blobName}");
            if (string.IsNullOrWhiteSpace(blobName))
            {
                _logger.LogWarning("Missing blob name in request");
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            _logger.LogInformation($"Function {nameof(DeleteImage)} was triggered");
            await _imageBlobService.DeleteBlobAsync(blobName);

            return request.CreateResponse(HttpStatusCode.OK);
        }
        #endregion

        #region BlogPostTrigger
        [Function(nameof(GetMarkdown))]
        [OpenApiOperation(operationId: nameof(GetMarkdown), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the markdown")]
        public async Task<HttpResponseData> GetMarkdown([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post/{slug}/markdown")] HttpRequestData request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetMarkdown)} was triggered for {slug}");
            if (String.IsNullOrEmpty(slug))
            {
                slug = System.Web.HttpUtility.ParseQueryString(request!.Url!.Query!)["slug"]!;
            }

            if (string.IsNullOrEmpty(slug) || string.IsNullOrWhiteSpace(slug))
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            string markdownText = await _markdownBlobService.GetMarkdownAsync(slug);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(markdownText, Encoding.UTF8);
            return response;
        }

        [Function(nameof(DeletePost))]
        [OpenApiOperation(operationId: nameof(DeletePost), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<HttpResponseData> DeletePost([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "post/{slug}")] HttpRequestData request, string slug)
        {
            _logger.LogInformation($"Function {nameof(DeletePost)} was triggered for {slug}");

            if (string.IsNullOrWhiteSpace(slug))
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            await _markdownBlobService.DeleteMarkdownAsync(slug);
            await _htmlBlobService.DeleteBlogHtmlContentAsync(slug);

            await _blogMetadataService.DeleteBlogPostAsync(slug);

            return request.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(GetBlogPosts))]
        [OpenApiOperation(operationId: nameof(GetBlogPosts), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<HttpResponseData> GetBlogPosts([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post")] HttpRequestData request)
        {
            _logger.LogInformation($"Function {nameof(GetBlogPosts)} was triggered");

            var metadata = await _blogMetadataService.GetBlogPostMetadataAsync();
            
            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(metadata);
            return response;
        }

        [Function(nameof(GetBlogPost))]
        [OpenApiOperation(operationId: nameof(GetBlogPost), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<HttpResponseData> GetBlogPost([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post/{slug}")] HttpRequestData request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetBlogPost)} was triggered");
            var metadata = await _blogMetadataService.GetBlogPostMetadataAsync(slug);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(metadata);
            return response;
        }

        [Function(nameof(SavePostContent))]
        [OpenApiOperation(operationId: nameof(SavePostContent), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("text/plain", typeof(string), Required = true)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<HttpResponseData> SavePostContent([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "post/{slug}")] HttpRequestData request, string slug)
        {
            _logger.LogInformation($"Function {nameof(SavePostContent)} was triggered");
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = System.Web.HttpUtility.ParseQueryString(request!.Url!.Query!)["slug"]!;
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            string content = string.Empty;
            using (Stream stream = request.Body)
            {
                using StreamReader reader = new(stream, Encoding.UTF8);
                content = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            content = Regex.Unescape(content);

            if (string.IsNullOrWhiteSpace(content))
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            await _markdownBlobService.UploadMarkdownAsync(content, slug);

            string body = System.Text.Json.JsonSerializer.Serialize(new QueueMessage() { Slug = slug});
            var sender = _serviceBusClient.CreateSender(ServiceBusQueueNames.NewBlogPostQueue);

            await sender.SendMessageAsync(new ServiceBusMessage(body));

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(slug);
            return response;
        }

        [Function(nameof(SchedulePostPublish))]
        [OpenApiOperation(operationId: nameof(SchedulePostPublish), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("application/json", typeof(string), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<HttpResponseData> SchedulePostPublish(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "publish")] HttpRequestData request)
        {
            _logger.LogInformation($"Function {nameof(SchedulePostPublish)} was triggered");
            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            PublishRequest publishRequest = JsonConvert.DeserializeObject<PublishRequest>(requestBody);
            _logger.LogInformation($"Received {nameof(PublishRequest)} for {publishRequest!.Slug} at {publishRequest.PublishDate}");

            await _blogMetadataService.ScheduleBlogPostPublishAsync(publishRequest);

            return request.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(SavePostMetadata))]
        [OpenApiOperation(operationId: nameof(SavePostMetadata), tags: new[] { "BlogPosts" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("application/json", typeof(string), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<HttpResponseData> SavePostMetadata([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "post")] HttpRequestData request)
        {
            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            PostMetadata metadata = JsonConvert.DeserializeObject<PostMetadata>(requestBody);
            await _blogMetadataService.SavePostMetadataAsync(metadata!);

            return request.CreateResponse(HttpStatusCode.OK);
        }
        #endregion

        #region MetricTrigger
        [Function(nameof(GetPageViewHistory))]
        [OpenApiOperation(operationId: nameof(GetPageViewHistory), tags: new[] { "Metric" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the page view history")]
        public async Task<HttpResponseData> GetPageViewHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric/{slug}/history")] HttpRequestData request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetPageViewHistory)} was triggered");
            if (string.IsNullOrWhiteSpace(slug))
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            var pageMetric = await _metricService.GetPageMetricAsync(slug);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(pageMetric);
            return response;
        }

        [Function(nameof(GetPageViews))]
        [OpenApiOperation(operationId: nameof(GetPageViews), tags: new[] { "Metric" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the page view history")]
        public async Task<HttpResponseData> GetPageViews([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric")] HttpRequestData request)
        {
            _logger.LogInformation($"Function {nameof(GetPageViews)} was triggered");
            var pageViews = await _metricService.GetPageViews();

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(pageViews);
            return response;
        }

        [Function(nameof(GetPageView))]
        [OpenApiOperation(operationId: nameof(GetPageView), tags: new[] { "Metric" })]
        [OpenApiSecurity("Azure AD Authentication", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow), Name = "Authorization", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "slug", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **slug** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response containing the page view history")]
        public async Task<HttpResponseData> GetPageView(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metric/{slug}")] HttpRequestData request, string slug)
        {
            _logger.LogInformation($"Function {nameof(GetPageView)} was triggered");

            var pageViews = await _metricService.GetPageViews(slug);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(pageViews);
            return response;
        }
        #endregion
    }
}