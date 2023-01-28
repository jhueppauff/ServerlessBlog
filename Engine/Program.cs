using Azure.Identity;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ServerlessBlog.Engine.Services;
using System;
using System.Threading.Tasks;

namespace ServerlessBlog.Engine
{
    public static class Program
    {
        public static async Task Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseNewtonsoftJson();
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
                        clientBuilder.AddServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnection"));
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
