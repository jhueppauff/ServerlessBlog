using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace EditorNG
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            string endpoint = builder.Configuration.GetSection("Backend").GetValue<string>("Endpoint");

            builder.Services.AddMsalAuthentication(options =>
            {
                builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
                options.ProviderOptions.DefaultAccessTokenScopes.Add($"{endpoint}/user_impersonation");
            });

            builder.Services.AddScoped<CustomAuthorizationMessageHandler>();

            builder.Services.AddHttpClient(nameof(BlogClient),
                client =>
                {
                    client.BaseAddress = new Uri(endpoint);

                }).AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

            builder.Services.AddTransient<BlogClient>();

            builder.Services.AddMudServices();

            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(BlogClient)));

            await builder.Build().RunAsync();
        }
    }
}
