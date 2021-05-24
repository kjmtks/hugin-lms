using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hugin.Services;
using Hugin.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Hugin
{

    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            CreateDbIfNotExists(host);
            host.Run();
        }

        private static void CreateDbIfNotExists(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<DatabaseContext>();
                    var userHandler = services.GetRequiredService<UserHandleService>();
                    var lectureHandler = services.GetRequiredService<LectureHandleService>();
                    var sandboxTemplateHandler = services.GetRequiredService<SandboxTemplateHandleService>();
                    DbInitializer.Initialize(context);
                    SeedData.InitializeAsync(context, userHandler, lectureHandler, sandboxTemplateHandler).Wait();
                }
                catch (Exception e)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(e, "An error occured creating the Database.");
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    if (env == "Production")
                    {
                        webBuilder.UseStartup<Startup>().UseUrls("https://*:443");
                    }
                    else
                    {
                        webBuilder.UseStartup<Startup>().UseUrls("http://*:8080");
                        //webBuilder.UseStartup<Startup>();
                    }
                });
    }
}
