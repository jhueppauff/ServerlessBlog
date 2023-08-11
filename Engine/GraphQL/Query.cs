using ServerlessBlog.Engine.Services;

namespace ServerlessBlog.Engine.GraphQL
{
    public class Query
    {
        private readonly MetricService _metricService;

        private readonly BlogMetadataService _metadataService;

        private readonly ImageBlobService _imageBlobService;

        private readonly MarkdownBlobService _markdownBlobService;

        public Query(MetricService metricService, BlogMetadataService metadataService, ImageBlobService imageBlobService, MarkdownBlobService markdownBlobService)
        {
            _metricService = metricService;
            _metadataService = metadataService;
            _imageBlobService = imageBlobService;
            _markdownBlobService = markdownBlobService;
        }

        public async Task<string> GetMarkdownAsync(string slug) => await _markdownBlobService.GetMarkdownAsync(slug);

        public async Task<List<Blob>> GetImagesAsync() => await _imageBlobService.GetImagesAsync();

        public async Task<PostMetadata> GetPostMetadataBySlugAsync(string slug) => await _metadataService.GetBlogPostMetadataAsync(slug);

        public async Task<List<PostMetadata>> GetPostMetadataAsync() => await _metadataService.GetBlogPostMetadataAsync();

        public async Task<List<PageView>> GetPageMetricsAsync(string slug) => await _metricService.GetPageMetricAsync(slug);

        public async Task<PageView> GetPageViewAsync(string slug) => await _metricService.GetPageViews(slug);
    }
}
