using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Diagnostics;


namespace Hugin.Models
{
    public enum PackService { GitUploadPack, GitReceivePack }

    public class Repository
    {
        public string Path { get; set; }
        public string BaredFullPath { get => $"{Path}/bared.git/"; }
        public string GetNonBaredFullPath(string branch) => $"{Path}/nobared/{branch}/";

        public IEnumerable<string> GetNonBaredFullPaths()
        {
            return Directory.GetDirectories($"{Path}/nobared/").Select(x => $"{Path}/nobared/{x}/");
        }

        public Repository(string path)
        {
            Path = path;
        }

    }

}
