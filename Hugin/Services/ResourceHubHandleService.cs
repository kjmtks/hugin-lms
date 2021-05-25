using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using Hugin.Data;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Hugin.Services
{
    public class ResourceHubHandleService : EntityHandleServiceBase<ResourceHub>
    {
        private readonly IHttpClientFactory ClientFactory;
        public ResourceHubHandleService(DatabaseContext databaseContext, IHttpClientFactory clientFactory) : base(databaseContext)
        {
            ClientFactory = clientFactory;
        }

        public override DbSet<ResourceHub> Set { get => DatabaseContext.ResourceHubs; }

        public override IQueryable<ResourceHub> DefaultQuery { get => Set; }

        protected override bool BeforeAddNew(ResourceHub model)
        {
            // TODO
            WgetResourceHubAsync(model.YamlURL).Wait();
            return true;
        }


        public async Task WgetResourceHubAsync(string yamlUrl)
        {
            var info = new System.Diagnostics.ProcessStartInfo();
            info.FileName = "curl";
            info.Arguments = yamlUrl;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            var proc = new System.Diagnostics.Process();
            proc.StartInfo = info;
            proc.Start();
            await proc.WaitForExitAsync();
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            // TODO
            var obj = deserializer.Deserialize<object>(proc.StandardOutput);
        }

    }
}
