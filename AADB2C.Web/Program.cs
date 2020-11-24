using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostBuilder, configurationBuilder) =>
    {
        var configuration = configurationBuilder.Build();
        configurationBuilder.AddAzureAppConfiguration(config =>
            config.Connect(configuration.GetConnectionString("AzureAppConfiguration")).ConfigureKeyVault(options =>
                {
                    TokenCredential credential = hostBuilder.HostingEnvironment.IsDevelopment() ? new SharedTokenCacheCredential() : new ManagedIdentityCredential();
                    options.SetCredential(credential);
                }));
    })
    .ConfigureWebHostDefaults(host =>
    {
        host.ConfigureServices((context, services) =>
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(context.Configuration);
            services.AddAuthorization();
            services.AddHealthChecks();
        })
        .Configure((context, app) =>
        {
            if (context.HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", ctx => ctx.Response.WriteAsync($"Hello, {ctx.User.Identity.Name}!")).RequireAuthorization();
                endpoints.MapHealthChecks("/health");
            });
        });
    }).Build().RunAsync();
