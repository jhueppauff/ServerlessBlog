using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using ServerlessBlog.McpServer;

var builder = Host.CreateApplicationBuilder(args);
var apiUrl = Environment.GetEnvironmentVariable("SERVERLESSBLOG_API_URL")
    ?? "https://func-blog-engine-we-prod-001.azurewebsites.net";

if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var apiUri) ||
    apiUri.Scheme is not ("http" or "https"))
{
    throw new InvalidOperationException("SERVERLESSBLOG_API_URL must be an absolute HTTP or HTTPS URL.");
}

var httpClient = new HttpClient { BaseAddress = new Uri($"{apiUri.ToString().TrimEnd('/')}/") };
var apiToken = Environment.GetEnvironmentVariable("SERVERLESSBLOG_API_TOKEN");
if (!string.IsNullOrWhiteSpace(apiToken))
{
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
}

builder.Services.AddSingleton(httpClient);
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<BlogTools>();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

await builder.Build().RunAsync();
