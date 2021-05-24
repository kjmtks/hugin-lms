using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Models
{
    public class ActivityProfile
    {
        public int Number { get; set; }
        public string UserAccount { get; set; }
        public string LectureOwnerAccount { get; set; }
        public string LectureName { get; set; }
        public string ActivityName { get; set; }
        public string ActivityRef { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public string PagePath { get; set; }
        public string Rivision { get; set; }

        public bool CanUseSubmit { get; set; }
        public bool CanUseAnswer { get; set; }
    }
}
