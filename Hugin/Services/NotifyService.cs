using System;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public abstract class NotifierService
    {
        public async Task Update()
        {
            if (Notify != null)
            {
                await Notify.Invoke();
            }
        }

        public event Func<Task> Notify;
    }

    public class JobQueueNotifierService : NotifierService { }

    public class SandboxNotifierService : NotifierService { }

    public class SubmissionNotifierService : NotifierService { }

}
