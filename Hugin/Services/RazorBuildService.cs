using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public class RazorBuildService
    {
        class Dummy { }

        private readonly RazorLight.RazorLightEngine engine;

        public RazorBuildService()
        {
            engine = new RazorLight.RazorLightEngineBuilder()
               .UseEmbeddedResourcesProject(typeof(Dummy))
               .UseMemoryCachingProvider()
               .DisableEncoding()
               .Build();
        }

        public async Task<string> CompileAsync<T>(string key, string text, T model, System.Dynamic.ExpandoObject viewbag)
        {
            return await engine.CompileRenderStringAsync(key, text, model, viewbag);
        }
    }
}
