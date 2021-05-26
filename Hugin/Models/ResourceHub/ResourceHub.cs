using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Models.ResourceHub
{
    public class ResourceHub
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public IEnumerable<Activity> Activities { get; set; }
        public IEnumerable<string> Sandboxes { get; set; }
        public IEnumerable<Content> Contents { get; set; }
    }

    public class Content
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }

    public class Sandbox
    {
        public string HubName { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
        public string Commands { get; set; }
    }
    public class Activity
    {
        public string HubName { get; set; }
        public string ExportName { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Requirements { get; set; }
        public string Xml { get; set; }
    }
}
