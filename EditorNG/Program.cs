using EditorNG.States;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace EditorNG
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            string endpoint = builder.Configuration.GetSection("Backend").GetValue<string>("Endpoint");

            builder.Services.AddMsalAuthentication(options =>
            {
                builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
                options.ProviderOptions.DefaultAccessTokenScopes.Add($"{endpoint}/user_impersonation");
            });

            builder.Services.AddScoped<CustomAuthorizationMessageHandler>();
            builder.Services.AddScoped<DarkModeState>();

            builder.Services.AddHttpClient(nameof(BlogClient),
                client =>
                {
                    client.BaseAddress = new Uri(endpoint);

                }).AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

            builder.Services.AddTransient<BlogClient>();

            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 10000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            });

            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(BlogClient)));

            await builder.Build().RunAsync();
        }
    }
}
