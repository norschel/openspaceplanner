using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using openspace.Hubs;
using openspace.Repositories;
using openspace.Services;
using System;
using System.IO;
using System.Net.Http;

namespace openspace
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value;
                if (!path.StartsWith("/api") && !path.StartsWith("/hubs") && !Path.HasExtension(path))
                {
                    context.Request.Path = "/index.html";
                }

                await next();
            });

            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();

            app.UseSignalR(routes =>
            {
                routes.MapHub<SessionsHub>("/hubs/sessions");
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSignalR();

            services.AddHttpClient();

            if (Configuration["TableStorageAccount"] != null)
            {
                services.AddSingleton<ISessionRepositoryV1>(_ => {
                    var repository = new SessionRepositoryV1(Configuration);
                    repository.InitializeAsync().GetAwaiter().GetResult();
                    return repository;
                });

                services.AddSingleton<ISessionRepositoryV2>(_ => {
                    var repository = new SessionRepositoryV2(Configuration);
                    repository.InitializeAsync().GetAwaiter().GetResult();
                    return repository;
                });
            }

            var hostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? "localhost:5001";
            var sessionUrlFormat = $"https://{hostName}/sessions/{{0}}";

            services.AddSingleton<ITeamsService>(provider =>
                new TeamsService(provider.GetService<IHttpClientFactory>(), Configuration["TeamsWebhookUrl"], sessionUrlFormat));

            services.AddSingleton<ICalendarServiceV1>(provider
                => new CalendarServiceV1(provider.GetService<ISessionRepositoryV1>(), Configuration["Timezone"] ?? "Europe/Berlin"));

            services.AddSingleton<ICalendarServiceV2>(provider
                => new CalendarServiceV2(provider.GetService<ISessionRepositoryV2>(), Configuration["Timezone"] ?? "Europe/Berlin"));
        }
    }
}
