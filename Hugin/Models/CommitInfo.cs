using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Models
{
    public class CommitInfo
    {
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public string Message { get; set; }
        public string Hash { get; set; }
        public string ShortHash { get; set; }
        public DateTime Date { get; set; }
    }

}
