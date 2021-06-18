using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Models
{
    public class ContentTypeProvider
    {
        public static string GetContentType(string path, string defaultType = "application/octet-stream")
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
            provider.Mappings[".svg"] = "image/svg+xml";
            provider.Mappings[".svgz"] = "image/svg+xml";
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = defaultType;
            }
            return contentType;
        }
        public static string GetLangType(string path, string defaultType = "text")
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".txt"] = "text";
            provider.Mappings[".md"] = "markdown";
            provider.Mappings[".css"] = "css";
            provider.Mappings[".js"] = "javascript";
            provider.Mappings[".htm"] = "html";
            provider.Mappings[".html"] = "html";
            provider.Mappings[".py"] = "python";
            provider.Mappings[".c"] = "c";
            provider.Mappings[".h"] = "c";
            provider.Mappings[".cpp"] = "c";
            provider.Mappings[".hs"] = "haskell";
            provider.Mappings[".cs"] = "csharp";
            provider.Mappings[".gitignore"] = "gitignore";
            provider.Mappings[".m"] = "matlab";
            provider.Mappings[".r"] = "r";
            provider.Mappings[".pl"] = "perl";
            provider.Mappings[".java"] = "java";
            provider.Mappings[".rs"] = "rust";
            provider.Mappings[".keep"] = "text";
            provider.Mappings[".rb"] = "ruby";
            provider.Mappings[".tex"] = "tex";
            provider.Mappings[".svg"] = "svg";
            provider.Mappings[".xml"] = "xml";
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = defaultType;
            }
            return contentType;
        }
    }
}
