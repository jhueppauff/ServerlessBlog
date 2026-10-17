using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ServerlessBlog.McpServer;

[McpServerToolType]
public sealed class BlogTools
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "list_blog_posts"), Description("List published ServerlessBlog posts and their metadata.")]
    public static async Task<string> ListBlogPosts(HttpClient client, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync("api/post", cancellationToken);
        response.EnsureSuccessStatusCode();

        var posts = await response.Content.ReadFromJsonAsync<List<BlogPostMetadata>>(cancellationToken: cancellationToken)
            ?? [];

        return JsonSerializer.Serialize(
            posts.Where(post => post.IsPublic)
                .Select(post => new PublishedPost(post.Slug, post.Title, post.Published, post.Tags, post.Preview, post.ImageUrl)),
            JsonOptions);
    }

    [McpServerTool(Name = "get_blog_post"), Description("Get a published ServerlessBlog post, including its metadata and Markdown content.")]
    public static async Task<string> GetBlogPost(
        HttpClient client,
        [Description("The exact slug of the published post.")] string slug,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new McpException("A post slug is required.");
        }

        using var metadataResponse = await client.GetAsync($"api/post/{Uri.EscapeDataString(slug)}", cancellationToken);
        metadataResponse.EnsureSuccessStatusCode();
        var metadata = await metadataResponse.Content.ReadFromJsonAsync<BlogPostMetadata>(cancellationToken: cancellationToken);

        if (metadata is null || !metadata.IsPublic)
        {
            throw new McpException("The post is not published or could not be found.");
        }

        using var markdownResponse = await client.GetAsync(
            $"api/post/{Uri.EscapeDataString(slug)}/markdown",
            cancellationToken);
        markdownResponse.EnsureSuccessStatusCode();
        var markdown = await markdownResponse.Content.ReadAsStringAsync(cancellationToken);
        var post = new PublishedPost(
            metadata.Slug,
            metadata.Title,
            metadata.Published,
            metadata.Tags,
            metadata.Preview,
            metadata.ImageUrl);

        return JsonSerializer.Serialize(new { metadata = post, markdown }, JsonOptions);
    }

    private sealed class BlogPostMetadata
    {
        public string? Slug { get; init; }
        public string? Title { get; init; }
        public string? Published { get; init; }
        public string? Tags { get; init; }
        public string? Preview { get; init; }
        public string? ImageUrl { get; init; }
        public bool IsPublic { get; init; }
    }

    private sealed record PublishedPost(
        string? Slug,
        string? Title,
        string? Published,
        string? Tags,
        string? Preview,
        string? ImageUrl);
}
