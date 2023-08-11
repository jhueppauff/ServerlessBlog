using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ServerlessBlog.Engine.GraphQL;
using ServerlessBlog.Engine.Services;

namespace ServerlessBlog.Engine
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseNewtonsoftJson();
                })
                .AddGraphQLFunction(b =>
                {
                    b.AddQueryType<Query>();
                    b.AddMutationType<Mutations>();
                })
                .ConfigureOpenApi()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
                            {
                                var options = new OpenApiConfigurationOptions()
                                {
                                    Info = new OpenApiInfo()
                                    {
                                        Version = "1.0.0",
                                        Title = "Serverless Blog Backend API",
                                        Description = "",
                                    },
                                    Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
                                    OpenApiVersion = DefaultOpenApiConfigurationOptions.GetOpenApiVersion(),
                                    IncludeRequestingHostName = true,
                                    ForceHttps = false,
                                    ForceHttp = false,
                                };

                                return options;
                            });
                    services.AddAzureClients(clientBuilder =>
                    {
                        clientBuilder.AddBlobServiceClient(Environment.GetEnvironmentVariable("AzureStorageConnection"));
                        clientBuilder.AddServiceBusClientWithNamespace(Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace"));
                        clientBuilder.AddTableServiceClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
                        clientBuilder.UseCredential(new DefaultAzureCredential());
                    });


                    services.AddTransient<ImageBlobService>();
                    services.AddTransient<BlogMetadataService>();
                    services.AddTransient<MetricService>();
                    services.AddTransient<HtmlBlobService>();
                    services.AddTransient<MarkdownBlobService>();

                    services.AddLogging();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
