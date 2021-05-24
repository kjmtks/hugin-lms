using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.ViewModels
{
    public class ActivityViewModel
    {
        public Guid Id { get; set; }

        public Models.Activity Activity { get; set; }
        public Models.ActivityProfile Profile { get; set; }

        public string EncryptedProfile { get; set; }

        public string Description { get; set; }

    }



}
