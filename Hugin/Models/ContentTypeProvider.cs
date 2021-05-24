using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Models
{
    public class ContentTypeProvider
    {
        public static string GetContentType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".txt"] = "text/plain";
            provider.Mappings[".md"] = "text/plain";
            provider.Mappings[".csv"] = "text/plain";
            provider.Mappings[".py"] = "text/plain";
            provider.Mappings[".c"] = "text/plain";
            provider.Mappings[".h"] = "text/plain";
            provider.Mappings[".cpp"] = "text/plain";
            provider.Mappings[".hs"] = "text/plain";
            provider.Mappings[".cs"] = "text/plain";
            provider.Mappings[".fs"] = "text/plain";
            provider.Mappings[".m"] = "text/plain";
            provider.Mappings[".r"] = "text/plain";
            provider.Mappings[".pl"] = "text/plain";
            provider.Mappings[".java"] = "text/plain";
            provider.Mappings[".ml"] = "text/plain";
            provider.Mappings[".rs"] = "text/plain";
            provider.Mappings[".keep"] = "text/plain";
            provider.Mappings[".rb"] = "text/plain";
            provider.Mappings[".css"] = "text/css";
            provider.Mappings[".js"] = "text/javascript";
            provider.Mappings[".html"] = "text/html";
            provider.Mappings[".htm"] = "text/html";
            provider.Mappings[".tex"] = "text/plain";
            provider.Mappings[".pdf"] = "application/pdf";
            provider.Mappings[".png"] = "image/png";
            provider.Mappings[".jpg"] = "image/jpeg";
            provider.Mappings[".jpeg"] = "image/jpeg";
            provider.Mappings[".gif"] = "image/gif";
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }
    }
}
