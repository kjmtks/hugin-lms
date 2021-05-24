using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public class ActivityActionStatusService
    {
        private Dictionary<string, Status> status = new Dictionary<string, Status>();

        public event Func<Task> Notify;

        public Status GetStatus(string account)
        {
            if (status.ContainsKey(account))
            {
                return status[account];
            }
            return null;
        }

        public async Task Record(string account, Status st)
        {
            if (status.ContainsKey(account))
            {
                status[account] = st;
            }
            else
            {
                status.Add(account, st);
            }
            if (Notify != null)
            {
                await Notify.Invoke();
            }
        }
        public class Status
        {
            public string LectureSubject { get; set; }
            public string LectureOwner { get; set; }
            public string LectureName { get; set; }
            public string PageName { get; set; }
            public string ActivityName { get; set; }
            public Models.ActivityActionTypes ActionType { get; set; }
            public string Summary { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
    }
}
