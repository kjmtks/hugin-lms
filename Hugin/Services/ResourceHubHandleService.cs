﻿using Microsoft.EntityFrameworkCore;
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
            var t = WgetResourceHubAsync(model.YamlURL);
            t.Wait();
            var x = t.Result;
            model.Name = x.Name;
            model.Description = x.Description;
            return true;
        }
        public async Task<string> WgetActivityXml(string url)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("User-Agent", "Hugin");
                using (var client = ClientFactory.CreateClient())
                {
                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public async Task<IEnumerable<Hugin.Models.ResourceHub.Sandbox>> WgetSandboxes(ResourceHub model)
        {
            var result = new List<Hugin.Models.ResourceHub.Sandbox>();
            var xs = await WgetResourceHubAsync(model.YamlURL);
            foreach(var x in xs.Sandboxes)
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, x))
                {
                    request.Headers.Add("User-Agent", "Hugin");
                    using (var client = ClientFactory.CreateClient())
                    {
                        using (var response = await client.SendAsync(request).ConfigureAwait(false))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                var yaml = await response.Content.ReadAsStringAsync();
                                if (!string.IsNullOrWhiteSpace(yaml))
                                {
                                    var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                                    var sandbox = deserializer.Deserialize<Hugin.Models.ResourceHub.Sandbox>(yaml);
                                    sandbox.HubName = model.Name;
                                    result.Add(sandbox);
                                }
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            return result;
        }


        public async Task<Hugin.Models.ResourceHub.ResourceHub> WgetResourceHubAsync(string yamlUrl)
        {

            using (var request = new HttpRequestMessage(HttpMethod.Get, yamlUrl))
            {
                request.Headers.Add("User-Agent", "Hugin");
                using (var client = ClientFactory.CreateClient())
                {
                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var yaml = await response.Content.ReadAsStringAsync();

                            if (!string.IsNullOrWhiteSpace(yaml))
                            {
                                var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                                return deserializer.Deserialize<Hugin.Models.ResourceHub.ResourceHub>(yaml);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
    }
}
