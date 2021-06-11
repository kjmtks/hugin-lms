using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Services
{

    public class ApplicationConfigurationService
    {
        private IConfiguration Configuration { get; set; }
        public ApplicationConfigurationService(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string GetAppDataPath() => Environment.GetEnvironmentVariable("APP_DATA_PATH");
        public string GetAppSecretKey() => Environment.GetEnvironmentVariable("APP_SECRET_KEY");
        public string GetEncryptKey() => Environment.GetEnvironmentVariable("APP_SECRET_KEY").Substring(Math.Min(32, Environment.GetEnvironmentVariable("APP_SECRET_KEY").Length));
        public string GetAppURL() => Environment.GetEnvironmentVariable("APP_URL");

        public int GetRenderTimeLimit() {
            var x = Environment.GetEnvironmentVariable("RENDER_TIME_LIMIT");
            if(!string.IsNullOrWhiteSpace(x) && !int.TryParse(x, out var s))
            {
                return s;
            }
            return 3;
        }

        public string GetAppName() => Environment.GetEnvironmentVariable("APP_NAME") ?? "Hugin";
        public string GetAppDescription() => Environment.GetEnvironmentVariable("APP_DESCRIPTION") ?? "";

        public int GetNumOfTeacherQueues() => int.TryParse(Environment.GetEnvironmentVariable("NUM_OF_TEACHER_QUEUES"), out int num) ? num : 3;
        public int GetNumOfNormalQueues() => int.TryParse(Environment.GetEnvironmentVariable("NUM_OF_NORMAL_QUEUES"), out int num) ? num : 5;
    }
}
