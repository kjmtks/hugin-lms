using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections;


namespace Hugin.Services
{
    public class QueuedHostedBackgroundJobService : BackgroundService
    {
        private ApplicationConfigurationService Conf;
        private IBackgroundTaskQueueSet TaskQueue;
        public QueuedHostedBackgroundJobService(IBackgroundTaskQueueSet taskQueue, ApplicationConfigurationService conf)
        {
            Conf = conf;
            TaskQueue = taskQueue;
            TaskQueue.SetQueues(conf.GetNumOfNormalQueues(), conf.GetNumOfTeacherQueues());
        }
        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await TaskQueue.ExecAsync(cancellationToken);
        }
    }

    public interface IBackgroundTaskQueueSet
    {
        public IEnumerable<BackgroundTaskQueue> GetQueues();
        public IEnumerable<BackgroundTaskQueue> GetPrioritiedQueues();

        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, Data.User user, string description = null, bool prioritied = false);
        Task ExecAsync(CancellationToken cancellationToken);
        void SetQueues(int number_of_queue, int number_of_prioritied_queues);
    }

    public class BackgroundTaskQueueSet : IBackgroundTaskQueueSet
    {
        private BackgroundTaskQueue[] queues = new BackgroundTaskQueue[0];
        private BackgroundTaskQueue[] prioritied_queues = new BackgroundTaskQueue[0];

        public readonly JobQueueNotifierService JobQueueNotifier;
        public BackgroundTaskQueueSet(JobQueueNotifierService jobQueueNotifier)
        {
            JobQueueNotifier = jobQueueNotifier;
        }

        public IEnumerable<BackgroundTaskQueue> GetQueues()
        {
            return queues;
        }

        public IEnumerable<BackgroundTaskQueue> GetPrioritiedQueues()
        {
            return prioritied_queues;
        }


        public void SetQueues(int number_of_queue, int number_of_prioritied_queues)
        {
            queues = new BackgroundTaskQueue[number_of_queue];
            prioritied_queues = new BackgroundTaskQueue[number_of_prioritied_queues];
            for (var i = 0; i < number_of_queue; i++)
            {
                queues[i] = new BackgroundTaskQueue();
            }
            for (var i = 0; i < number_of_prioritied_queues; i++)
            {
                prioritied_queues[i] = new BackgroundTaskQueue();
            }
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, Data.User user, string description = null, bool prioritied = false)
        {
            if (prioritied)
            {
                var queue = prioritied_queues.OrderBy(q => q.Count + (q.IsRunning ? 1 : 0)).FirstOrDefault();
                if (queue != null)
                {
                    queue.QueueBackgroundWorkItem(workItem, user, description);
                }
            }
            else
            {
                var queue = queues.OrderBy(q => q.Count + (q.IsRunning ? 1 : 0)).FirstOrDefault();
                if (queue != null)
                {
                    queue.QueueBackgroundWorkItem(workItem, user, description);
                }
            }
        }

        public async Task ExecAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                foreach (var queue in queues.Concat(prioritied_queues))
                {
                    Task.Run(async () =>
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var job = await queue.DequeueAsync(cancellationToken);
                            queue.IsRunning = true;
                            queue.User = job.Item2;
                            queue.Description = job.Item3;
                            queue.StartedAt = DateTime.Now;
                            await JobQueueNotifier.Update();
                            await job.Item1(cancellationToken);
                            queue.IsRunning = false;
                            queue.User = null;
                            queue.Description = null;
                            await JobQueueNotifier.Update();
                        }
                    });
                }
            });
        }
    }






    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, Data.User user, string description);

        Task<(Func<CancellationToken, Task>, Data.User, string)> DequeueAsync(CancellationToken cancellationToken);

        int Count { get; }
        bool IsRunning { get; }
        DateTime StartedAt { get; }
        string Description { get; }
        Data.User User { get; }
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private ConcurrentQueue<(Func<CancellationToken, Task>, Data.User, string)> _workItems =
            new ConcurrentQueue<(Func<CancellationToken, Task>, Data.User, string)>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public int Count { get { return _signal.CurrentCount; } }
        public bool IsRunning { get; set; } = false;

        public DateTime StartedAt { get; set; }
        public string Description { get; set; }
        public Data.User User { get; set; }
        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, Data.User user, string description)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }
            _workItems.Enqueue((workItem, user, description));
            _signal.Release();
        }

        public async Task<(Func<CancellationToken, Task>, Data.User, string)> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }
}
