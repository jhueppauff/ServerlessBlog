using ServerlessBlog.Engine.Services;

namespace ServerlessBlog.Engine.GraphQL
{
    internal class Mutations
    {
        private readonly BlogMetadataService _metadataService;

        private readonly ImageBlobService _imageBlobService;

        private readonly MarkdownBlobService _markdownBlobService;

        public Mutations(BlogMetadataService metadataService, ImageBlobService imageBlobService, MarkdownBlobService markdownBlobService)
        {
            _metadataService = metadataService;
            _imageBlobService = imageBlobService;
            _markdownBlobService = markdownBlobService;
        }

        public async Task UpdateMarkdownAsync(string slug, string markdown) => await _markdownBlobService.UploadMarkdownAsync(markdown, slug);

        public async Task DeleteImageAsync(string blobName) => await _imageBlobService.DeleteBlobAsync(blobName);

        public async Task UpdateMetadataAsync(PostMetadata postMetadata) => await _metadataService.SavePostMetadataAsync(postMetadata);

        public async Task SchedulePublishAsync(PublishRequest publishRequest) => await _metadataService.ScheduleBlogPostPublishAsync(publishRequest);

        public async Task PublishPostAsync(string slug) => await _metadataService.PublishBlogPostAsync(slug);

        public async Task SaveBlogPostAsync(PostMetadata postMetadata, string markdown)
        {
            await _markdownBlobService.UploadMarkdownAsync(markdown, postMetadata.Slug);
            await _metadataService.SavePostMetadataAsync(postMetadata);
        }

        public async Task DeleteBlogPostAsync(string slug)
        {
            await _metadataService.DeleteBlogPostAsync(slug);
            await _markdownBlobService.DeleteMarkdownAsync(slug);
        }
    }
}
