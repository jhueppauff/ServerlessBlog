using Azure.Identity;
using Engine.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

[assembly: FunctionsStartup(typeof(Engine.Startup))]

namespace Engine
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();

            builder.Services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddBlobServiceClient(new Uri(Environment.GetEnvironmentVariable("AzureStorageConnection")));
                clientBuilder.AddTableServiceClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
                clientBuilder.UseCredential(new DefaultAzureCredential());
            });

            builder.Services.AddTransient<ImageBlobService>();
            builder.Services.AddTransient<BlogMetadataService>();
            builder.Services.AddTransient<MetricService>();
            builder.Services.AddTransient<HtmlBlobService>();
            builder.Services.AddTransient<MarkdownBlobService>();
        }
    }
}
