using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Net.Http.Headers;

[assembly: FunctionsStartup(typeof(AADB2C.Functions.Startup))]

namespace AADB2C.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<ExtensionService>();
            builder.Services.AddSingleton<AuthorizationService>();

            builder.Services.AddHttpClient<IApiService, ApiService>((provider, client) => client.BaseAddress = provider.GetRequiredService<IConfiguration>().GetValue<Uri>("API_SERVICE_URI"));

            builder.Services.AddSingleton(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();

                var tenantId = configuration.GetValue<string>("AAD_B2C_TENANT_ID");
                var clientId = configuration.GetValue<string>("B2C_CLIENT_ID");
                var clientSecret = configuration.GetValue<string>("B2C_CLIENT_SECRET");
                var redirectUri = configuration.GetValue<Uri>("B2C_REDIRECT_URI");

                var tenant = configuration.GetValue<string>("B2C_TENANT_NAME");
                var policy = configuration.GetValue<string>("B2C_POLICY_NAME");
                
                var authority = $"https://{tenant}.b2clogin.com/tfp/{tenant}.onmicrosoft.com/{policy}";

                var confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(clientId).WithTenantId(tenantId).WithClientSecret(clientSecret).WithB2CAuthority(authority).WithRedirectUri(redirectUri.AbsoluteUri).Build();
                return confidentialClientApplication;
            });

            builder.Services.AddSingleton<IGraphServiceClient>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();

                var tenantId = configuration.GetValue<string>("AAD_B2C_TENANT_ID");
                var clientId = configuration.GetValue<string>("AAD_CLIENT_ID");
                var clientSecret = configuration.GetValue<string>("AAD_CLIENT_SECRET");

                var confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(clientId).WithTenantId(tenantId).WithClientSecret(clientSecret).Build();
                var delegateAuthenticationProvider = new DelegateAuthenticationProvider(async request =>
                {
                    var token = await confidentialClientApplication.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync();
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(token.CreateAuthorizationHeader());
                });
                return new GraphServiceClient(delegateAuthenticationProvider);
            });
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var configuration = builder.ConfigurationBuilder.AddEnvironmentVariables().Build();
            var azureAppConfigurationConnectionString = configuration.GetValue<string>("AzureAppConfigurationConnectionString");
            if (azureAppConfigurationConnectionString != default)
            {
                TokenCredential tokenCredential = builder.GetContext().EnvironmentName == EnvironmentName.Development ? new SharedTokenCacheCredential() : new ManagedIdentityCredential();
                builder.ConfigurationBuilder.AddAzureAppConfiguration(configuration =>
                    configuration.Connect(azureAppConfigurationConnectionString).ConfigureKeyVault(options =>
                        options.SetCredential(tokenCredential)));
            }
        }
    }
}
