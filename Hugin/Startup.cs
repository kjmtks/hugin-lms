using Hugin.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hugin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Hugin.Hubs;
using Microsoft.AspNetCore.Localization;
using Polly.Extensions.Http;
using System.Net.Http;
using Polly;

namespace Hugin
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options => {
                options.CheckConsentNeeded = context => !context.User.Identity.IsAuthenticated;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
                options.HttpOnly = HttpOnlyPolicy.Always;
            });

            var subdir = Environment.GetEnvironmentVariable("SUB_DIR") ?? "";
            services.ConfigureApplicationCookie(options => { options.Cookie.Path = subdir; });

            services
               .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(options => { options.LoginPath = $"{subdir}/Login"; options.LogoutPath = $"{subdir}/Logout"; });

            var pathToProtectionKey = Environment.GetEnvironmentVariable("PATH_TO_PROTECTION_KEY");
	    if(!string.IsNullOrWhiteSpace(pathToProtectionKey))
            {
                services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(pathToProtectionKey));
            }
            services.AddDbContext<DatabaseContext>(options => options.UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION_STR")));

            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddSignalR(c => { c.MaximumReceiveMessageSize = 50 * 1024 * 1024; });

            services.AddHttpClient<ResourceHubHandleService>().SetHandlerLifetime(TimeSpan.FromMinutes(5)).AddPolicyHandler(GetRetryPolicy());

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddHostedService<QueuedHostedBackgroundJobService>();
            services.AddSingleton<IBackgroundTaskQueueSet, BackgroundTaskQueueSet>();

            services.AddScoped<UserHandleService>();
            services.AddScoped<ResourceHubHandleService>();
            services.AddScoped<LectureHandleService>();
            services.AddScoped<SandboxHandleService>();
            services.AddScoped<ActivityActionHandleService>();
            services.AddScoped<SubmissionHandleService>();

            services.AddScoped<PermissionProviderService>();

            services.AddScoped<ContentParserService>();
            services.AddScoped<ActivityHandleService>();
            services.AddScoped<SandboxExecutionService>(); 
            services.AddScoped<ActivityEncryptService>();



            services.AddSingleton<ApplicationConfigurationService>();
            services.AddSingleton<FilePathResolveService>();
            services.AddSingleton<ActivityEncryptService>();

            //services.AddSingleton<IContentsBuildService, RazorContentsBuildService>();
            services.AddSingleton<IContentsBuildService, ScribanContentsBuildService >();
            services.AddSingleton<RepositoryHandleService>();
           
            services.AddSingleton<JobQueueNotifierService>();
            services.AddSingleton<SandboxNotifierService>();
            services.AddSingleton<SubmissionNotifierService>();
            services.AddSingleton<ActivityActionStatusService>();
            services.AddSingleton<OnlineStatusService>();

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddMvc()
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var supportedCultures = new[] { "ja", "en" };
            var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            app.UseRequestLocalization(localizationOptions);

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "node_modules")),
                RequestPath = "/node_modules"
            });
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();

                var x = "";
                for(int i=0; i<28 - 2; i++)
                {
                    endpoints.MapFallbackToPage($"/SystemAdmin/Files/{x}{{Path{i+1}?}}", "/_Host");
                    x = $"{x}{{Path{i + 1}}}/";
                }

                var y = "";
                for (int i = 0; i < 28 - 5; i++)
                {
                    endpoints.MapFallbackToPage($"/LectureAdmin/{{Account}}/{{LectureName}}/ContentsRepository/{{Rivision}}/{y}{{Path{i + 1}?}}", "/_Host");
                    y = $"{y}{{Path{i + 1}}}/";
                }
                
                var z = "";
                for (int i = 0; i < 28 - 4; i++)
                {
                    endpoints.MapFallbackToPage($"/LectureAdmin/{{Account}}/{{LectureName}}/Submissions/{z}{{Path{i + 1}?}}", "/_Host");
                    z = $"{z}{{Path{i + 1}}}/";
                }

                var w = "";
                for (int i = 0; i < 28 - 5; i++)
                {
                    endpoints.MapFallbackToPage($"/LectureAdmin/{{Account}}/{{LectureName}}/UserSubmissions/{{UserAccount}}/{w}{{Path{i + 1}?}}", "/_Host");
                    w = $"{w}{{Path{i + 1}}}/";
                }

                var v = "";
                for (int i = 0; i < 28 - 3; i++)
                {
                    endpoints.MapFallbackToPage($"/MySubmission/{{Account}}/{{LectureName}}/{v}{{Path{i + 1}?}}", "/_Host");
                    v = $"{v}{{Path{i + 1}}}/";
                }

                var u = "";
                for (int i = 0; i < 28 - 6; i++)
                {
                    endpoints.MapFallbackToPage($"/LectureAdmin/{{Account}}/{{LectureName}}/UserDataRepository/{{UserAccount}}/{{Rivision}}/{u}{{Path{i + 1}?}}", "/_Host");
                    u = $"{u}{{Path{i + 1}}}/";
                }

                endpoints.MapHub<ActivityHub>("/activityHub");

                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
