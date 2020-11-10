using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Net.Http.Headers;

[assembly: FunctionsStartup(typeof(AADB2C.Startup))]

namespace AADB2C
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddMemoryCache();

            builder.Services.AddSingleton(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();

                var tenantId = configuration.GetValue<string>("AAD_TENANT_ID");
                var clientId = configuration.GetValue<string>("AAD_CLIENT_ID");
                var clientSecret = configuration.GetValue<string>("AAD_CLIENT_SECRET");
                var redirectUri = configuration.GetValue<Uri>("AAD_REDIRECT_URI");

                var tenant = configuration.GetValue<string>("AAD_B2C_TENANT");
                var policy = configuration.GetValue<string>("AAD_B2C_POLICY");
                
                var authority = $"https://{tenant}.b2clogin.com/tfp/{tenant}.onmicrosoft.com/{policy}";

                var confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(clientId).WithTenantId(tenantId).WithClientSecret(clientSecret).WithB2CAuthority(authority).WithRedirectUri(redirectUri.AbsoluteUri).Build();
                return confidentialClientApplication;
            });

            builder.Services.AddSingleton<IGraphServiceClient>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();

                var tenantId = configuration.GetValue<string>("AAD_TENANT_ID");
                var clientId = configuration.GetValue<string>("AAD_CLIENT_ID");
                var clientSecret = configuration.GetValue<string>("AAD_CLIENT_SECRET");

                var confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(clientId).WithTenantId(tenantId).WithClientSecret(clientSecret).Build();
                var delegateAuthenticationProvider = new DelegateAuthenticationProvider(async request =>
                {
                    var token = await confidentialClientApplication.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync();
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                });
                return new GraphServiceClient(delegateAuthenticationProvider);
            });
        }
    }
}
