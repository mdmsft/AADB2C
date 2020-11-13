using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;

await Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(host =>
    {
        host.ConfigureServices((context, services) =>
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(context.Configuration);

            services.AddAuthorization();
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

            app.UseEndpoints(endpoints => endpoints.MapGet("/", ctx => ctx.Response.WriteAsync($"Hello, {ctx.User.Identity.Name}!")).RequireAuthorization());
        });
    }).Build().RunAsync();
