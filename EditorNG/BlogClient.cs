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

        public async Task<List<PostMetadata>> GetBlogPostsAsync()
        {
            var response = await client.GetAsync("/api/post");
            List<PostMetadata> posts = JsonConvert.DeserializeObject<List<PostMetadata>>(await response.Content.ReadAsStringAsync());

            return posts;
        }

        public async Task<PostMetadata> GetBlogPostAsync(string slug)
        {
            var response = await client.GetAsync($"/api/post/{slug}");
            response.EnsureSuccessStatusCode();

            PostMetadata post = JsonConvert.DeserializeObject<PostMetadata>(await response.Content.ReadAsStringAsync());

            return post;
        }

        public async Task<string> GetBlogPostMarkdownAsync(string slug)
        {
            
            var response = await client.GetAsync($"/api/post/{slug}/markdown");
            response.EnsureSuccessStatusCode();

            string markdown = await response.Content.ReadAsStringAsync();

            return markdown;
        }

        public async Task<PageMetric> GetPageViewAsync(string slug)
        {
            var response = await client.GetAsync($"/api/metric/{slug}");
            response.EnsureSuccessStatusCode();

            PageMetric metric = JsonConvert.DeserializeObject<PageMetric>(await response.Content.ReadAsStringAsync());

            return metric;
        }

        public async Task<List<PageMetric>> GetPageViewsAsync()
        {
            var response = await client.GetAsync($"/api/metric");
            response.EnsureSuccessStatusCode();

            List<PageMetric> metric = JsonConvert.DeserializeObject<List<PageMetric>>(await response.Content.ReadAsStringAsync());

            return metric;
        }

        public int GetPageView(string slug)
        {
            var task = Task.Run(() => client.GetAsync($"/api/metric/{slug}"));
            task.Wait();

            var response = task.Result;
            response.EnsureSuccessStatusCode();

            var task2 = Task.Run(() => response.Content.ReadAsStringAsync());
            task2.Wait();
            string content = task2.Result;
            PageMetric metric = JsonConvert.DeserializeObject<PageMetric>(content);

            return metric.Views;
        }

        public async Task SaveBlogPostAsync(PostMetadata post, string markdown)
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

        public async Task DeleteBlogPostAsync(PostMetadata post)
        {
            var response = await client.DeleteAsync($"/api/post/{post.Slug}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Blob>> GetBlobsAsync()
        {
            var response = await client.GetAsync("/api/image");
            response.EnsureSuccessStatusCode();
            List<Blob> blobs = JsonConvert.DeserializeObject<List<Blob>>(await response.Content.ReadAsStringAsync());
            return blobs;
        }

        public async Task DeleteBlobAsync(Blob blob)
        {
            var response = await client.DeleteAsync($"/api/image/{blob.Name}");
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