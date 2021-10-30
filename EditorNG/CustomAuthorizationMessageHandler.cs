using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;

namespace EditorNG
{
    internal class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        public CustomAuthorizationMessageHandler(IAccessTokenProvider provider,
        NavigationManager navigationManager, IConfiguration configuration)
        : base(provider, navigationManager)
        {
            ConfigureHandler(
                authorizedUrls: new[] { configuration.GetSection("Backend").GetValue<string>("Endpoint") },
                scopes: new[] { $"{configuration.GetSection("Backend").GetValue<string>("Endpoint")}/user_impersonation" });
        }
    }
}