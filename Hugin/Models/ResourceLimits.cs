using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Models
{
    public partial class ResourceLimits
    {
        public uint CpuTime { get; set; }
        public string Memory { get; set; }
        public uint StdoutLength { get; set; }
        public uint StderrLength { get; set; }
        public uint Pids { get; set; }
    }
}
