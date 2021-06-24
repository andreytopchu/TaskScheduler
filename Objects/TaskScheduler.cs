using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Objects
{
    public class TaskScheduler:IJobExecutor
    {

        private volatile bool _isQueueProcessingComplete;
        private volatile bool _isAllProcessingComplete;
        private volatile bool _queueIsEmpty;

        private ConcurrentQueue<Action> _queueActions;
        private ConcurrentDictionary<Guid, Action> _runningTasks;

        private const int StopDefaultTimeout = 3000;
        private const int RefreshTimeout = 10;

        public int Amount => _queueActions.Count;
        public int RunningTasksCount => _runningTasks.Count;

        public TaskScheduler()
        {
            _queueActions = new ConcurrentQueue<Action>();
            _runningTasks = new ConcurrentDictionary<Guid, Action>();
        }

        public void Start(int maxConcurrent)
        {
            if (maxConcurrent <= 0 || maxConcurrent > 300)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrent));
            }

            Thread startThread = new Thread(start:() =>
            {
                _isQueueProcessingComplete = false;

                int freeSpace = maxConcurrent - RunningTasksCount;
                SendingMaximumTasksForExecution(freeSpace);

                while (!_isQueueProcessingComplete && !_isAllProcessingComplete)
                {
                    freeSpace = WaitSomeMillisecondsAndGetFreeSpaceInRunningTasks(RefreshTimeout,
                        maxConcurrent);
                    if (freeSpace > 0)
                        SendingMaximumTasksForExecution(freeSpace);

                    if (_queueIsEmpty && RunningTasksCount == 0)
                        _isAllProcessingComplete = true;
                }
            });
            startThread.Start();
    }

        public void Stop()
        {
            _isQueueProcessingComplete = true;

            while (RunningTasksCount != 0)
            {
                Thread.Sleep(RefreshTimeout);
            }

            if (_queueIsEmpty) return;
            Thread.Sleep(StopDefaultTimeout);
            if (_isQueueProcessingComplete)
                _isAllProcessingComplete = true;
        }

        public void Add(Action action)
        {
            if (action == null) throw new ArgumentNullException();

            _queueActions.Enqueue(action);
            _queueIsEmpty = false;
        }

        public void Clear()
        {
            if(_queueIsEmpty) return;

            while (_queueIsEmpty)
            {
                _queueActions.TryDequeue(out _);
                if (Amount == 0) _queueIsEmpty = true;
            }
        }

        public void LetTheSchedulerFinishCurrentSession()
        {
            while (!_isQueueProcessingComplete && !_isAllProcessingComplete)
            {
                Thread.Sleep(RefreshTimeout);
            }
        }

        private void SendingMaximumTasksForExecution(int freeSpace)
        {
            if (Amount == 0)
            {
                _queueIsEmpty = true; return;
            }

            int count = Amount <= freeSpace ? Amount : freeSpace;
            for (int i = 0; i < count; i++)
            {
                MoveNextActionToRunningTasksFromQueue();
            }
        }

        private void MoveNextActionToRunningTasksFromQueue()
        {
            if (Amount == 0)
            {
                _queueIsEmpty = true; return;
            }

            ThreadPool.QueueUserWorkItem(state =>
            {
                var id = Guid.NewGuid();
                _queueActions.TryDequeue(out var action);
                _runningTasks[id] = action;

                try
                {
                    action?.Invoke();
                }
                finally
                {
                    _runningTasks.TryRemove(id, out _);
                }
            });
        }

        private int WaitSomeMillisecondsAndGetFreeSpaceInRunningTasks(int millisecondsCount,
            int maxConcurrent)
        {
            Thread.Sleep(millisecondsCount);
            return maxConcurrent - RunningTasksCount;
        }
    }
}
