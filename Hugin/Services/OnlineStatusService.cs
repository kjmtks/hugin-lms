using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Services
{

    public class OnlineStatusService
    {
        private Dictionary<string, Status> status = new Dictionary<string, Status>();

        public event Func<Task> Notify;

        public int GetNumOfActiveUser()
        {
            return status.Where(x => x.Value.IsOnline).Count();
        }

        public Status GetStatus(string account)
        {
            if (status.ContainsKey(account))
            {
                return status[account];
            }
            return null;
        }

        public async Task VisitContentPageAsync(string lectureOwner, string lectureName, string lectureSubject, string page_name, string userAccount)
        {
            var st = new Status { IsOnline = true, UpdatedAt = DateTime.Now, LectureOwner = lectureOwner, LectureName = lectureName, LectureSubject = lectureSubject, PageName = page_name };
            if (status.ContainsKey(userAccount))
            {
                status[userAccount] = st;
            }
            else
            {
                status.Add(userAccount, st);
            }
            if (Notify != null)
            {
                await Notify.Invoke();
            }
        }

        public async Task LeaveContentPageAsync(string userAccount)
        {
            if (status.ContainsKey(userAccount))
            {
                var x = status[userAccount];
                x.IsOnline = false;
                x.UpdatedAt = DateTime.Now;
                status[userAccount] = x;
            }
            else
            {
                status.Add(userAccount, new Status { IsOnline = false, UpdatedAt = DateTime.Now, LectureSubject = null, LectureOwner = null, LectureName = null, PageName = null });
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
            public bool IsOnline { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
    }

}
