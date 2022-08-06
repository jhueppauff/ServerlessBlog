using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using EditorNG.Models;

namespace EditorNG
{
    public class BlogClient
    {
        private readonly HttpClient client;

        public BlogClient(IHttpClientFactory factory)
        {
            client = factory.CreateClient(nameof(BlogClient));
        }

        public async Task<List<PostMetadata>> GetBlogPosts()
        {
            var response = await client.GetAsync("/api/post");
            List<PostMetadata> posts = JsonConvert.DeserializeObject<List<PostMetadata>>(await response.Content.ReadAsStringAsync());

            return posts;
        }

        public async Task<PostMetadata> GetBlogPost(string slug)
        {
            var response = await client.GetAsync($"/api/post/{slug}");
            response.EnsureSuccessStatusCode();

            PostMetadata post = JsonConvert.DeserializeObject<PostMetadata>(await response.Content.ReadAsStringAsync());

            return post;
        }

        public async Task<string> GetBlogPostMarkdown(string slug)
        {
            
            var response = await client.GetAsync($"/api/post/{slug}/markdown");
            response.EnsureSuccessStatusCode();

            string markdown = await response.Content.ReadAsStringAsync();

            return markdown;
        }

        public async Task SaveBlogPost(PostMetadata post, string markdown)
        {
            List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();

            HttpRequestMessage savePostMetadataMessage = new HttpRequestMessage(HttpMethod.Post, "/api/post/");
            savePostMetadataMessage.Content = new StringContent(JsonConvert.SerializeObject(post));
            tasks.Add(client.SendAsync(savePostMetadataMessage));

            HttpRequestMessage savePostMarkdownMessage = new HttpRequestMessage(HttpMethod.Put, $"/api/post/{post.Slug}");
            savePostMarkdownMessage.Content = new StringContent(markdown);
            tasks.Add(client.SendAsync(savePostMarkdownMessage));

            await Task.WhenAll(tasks);

            foreach (Task<HttpResponseMessage> task in tasks)
            {
                task.Result.EnsureSuccessStatusCode();
            }
        }

        public async Task DeleteBlogPost(PostMetadata post)
        {
            var response = await client.DeleteAsync($"/api/post/{post.Slug}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<HttpResponseMessage> UploadFileAsync(IBrowserFile file, string extentsion)
        {
            long maxFileSize = 1024 * 1024 * 200;
            
            using MultipartFormDataContent content = new();

            StreamContent fileContent = new(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            content.Add(
                content: fileContent,
                name: "\"files\"",
                fileName: file.Name
            );

            HttpResponseMessage responseMessage = await client.PutAsync($"/api/image/upload/{extentsion}", content).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();

            return responseMessage;
        }
    }
}