﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DQueue.Interfaces;

namespace DQueue
{
    public class QueueConsumer<TMessage> : IDisposable
        where TMessage : new()
    {
        #region Helpers
        private class DispatchModel
        {
            public Task ParentTask { get; set; }
            public object Locker { get; set; }
            public List<Task> Tasks { get; set; }
            public CancellationTokenSource CTS { get; set; }
        }

        private class DispatchState<T>
        {
            public DispatchContext<T> Context { get; set; }
            public Action<DispatchContext<T>> Handler { get; set; }
        }

        private class ReceiveState<T>
        {
            public string QueueName { get; set; }
            public IQueueProvider Provider { get; set; }
            public Action<ReceptionContext<T>> Handler { get; set; }
            public CancellationToken Token { get; set; }
        }
        #endregion

        private readonly int _threads;
        private readonly string _queueName;
        private readonly List<Action<DispatchContext<TMessage>>> _handlers;
        private readonly List<Action<DispatchContext<TMessage>>> _completeHandlers;

        private readonly CancellationTokenSource _cts;
        private readonly Dictionary<int, DispatchModel> _tasks;

        public QueueConsumer()
            : this(1)
        {
        }

        public QueueConsumer(int threads)
            : this(null, threads)
        {
        }

        public QueueConsumer(string queueName)
            : this(queueName, 1)
        {
        }

        public QueueConsumer(string queueName, int threads)
        {
            _threads = threads;
            _queueName = queueName ?? QueueHelpers.GetQueueName<TMessage>();

            if (_threads <= 0)
            {
                throw new ArgumentOutOfRangeException("threads");
            }

            if (string.IsNullOrWhiteSpace(_queueName))
            {
                throw new ArgumentNullException("queueName");
            }

            _handlers = new List<Action<DispatchContext<TMessage>>>();
            _completeHandlers = new List<Action<DispatchContext<TMessage>>>();

            _cts = new CancellationTokenSource();
            _tasks = new Dictionary<int, DispatchModel>();
        }

        public string QueueName
        {
            get
            {
                return _queueName;
            }
        }

        public int Threads
        {
            get
            {
                return _threads;
            }
        }

        public QueueConsumer<TMessage> Receive(Action<DispatchContext<TMessage>> handler)
        {
            CheckDisposed();

            if (handler != null)
            {
                _handlers.Add(handler);
            }

            if (_tasks.Count < _threads)
            {
                for (var i = 0; i < _threads; i++)
                {
                    var provider = QueueHelpers.CreateProvider(QueueProvider.Configured);

                    var task = Task.Factory.StartNew((state) =>
                    {
                        var param = (ReceiveState<TMessage>)state;
                        param.Provider.Dequeue<TMessage>(
                            param.QueueName,
                            param.Handler,
                            param.Token);
                    },
                    new ReceiveState<TMessage>
                    {
                        QueueName = _queueName,
                        Provider = provider,
                        Handler = Dispatch,
                        Token = _cts.Token
                    },
                    _cts.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);

                    _tasks.Add(task.Id, new DispatchModel { ParentTask = task });
                }

                _cts.Token.Register(() =>
                {
                    foreach (var item in _tasks)
                    {
                        var dispatch = item.Value;

                        if (dispatch.CTS != null)
                        {
                            dispatch.CTS.Cancel();
                            dispatch.CTS.Dispose();
                        }

                        if (dispatch.Tasks != null)
                        {
                            dispatch.Tasks.Clear();
                        }
                    }
                });
            }

            return this;
        }

        private void Dispatch(ReceptionContext<TMessage> receptionContext)
        {
            var currentTaskId = Task.CurrentId;
            if (currentTaskId.HasValue && _tasks.ContainsKey(currentTaskId.Value))
            {
                var dispatch = _tasks[currentTaskId.Value];

                dispatch.Locker = new object();
                dispatch.Tasks = new List<Task>();
                dispatch.CTS = new CancellationTokenSource();

                var dispatchContext = new DispatchContext<TMessage>(
                    receptionContext.Message, dispatch.CTS.Token, (sender, status) =>
                {
                    if (status == DispatchStatus.Complete)
                    {
                        Continue(receptionContext, sender, dispatch);
                    }
                });

                foreach (var handler in _handlers)
                {
                    var task = Task.Factory.StartNew((state) =>
                    {
                        var param = (DispatchState<TMessage>)state;

                        try
                        {
                            param.Handler(param.Context);
                        }
                        catch (Exception ex)
                        {
                            param.Context.LogException(ex);
                        }
                    },
                    new DispatchState<TMessage>
                    {
                        Handler = handler,
                        Context = dispatchContext,
                    },
                    dispatch.CTS.Token,
                    TaskCreationOptions.AttachedToParent,
                    TaskScheduler.Default);

                    dispatch.Tasks.Add(task);
                }

                Task.Factory.ContinueWhenAll(dispatch.Tasks.ToArray(), (t) =>
                {
                    Continue(receptionContext, dispatchContext, dispatch);
                });
            }
        }

        private void Continue(ReceptionContext<TMessage> receptionContext, DispatchContext<TMessage> dispatchContext, DispatchModel dispatch)
        {
            if (!dispatch.CTS.IsCancellationRequested)
            {
                lock (dispatch.Locker)
                {
                    if (!dispatch.CTS.IsCancellationRequested)
                    {
                        foreach (var handler in _completeHandlers)
                        {
                            try
                            {
                                handler(dispatchContext);
                            }
                            catch
                            {
                            }
                        }

                        dispatch.CTS.Cancel();
                        dispatch.CTS.Dispose();
                        dispatch.Tasks.Clear();

                        receptionContext.Success();
                    }
                }
            }
        }

        public QueueConsumer<TMessage> Complete(Action<DispatchContext<TMessage>> handler)
        {
            CheckDisposed();

            if (handler != null)
            {
                _completeHandlers.Add(handler);
            }

            return this;
        }

        private void CheckDisposed()
        {
            if (_cts == null || _cts.IsCancellationRequested)
            {
                throw new InvalidOperationException("Consumer already disposed");
            }
        }

        public void Dispose()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }

            _tasks.Clear();

            _handlers.Clear();

            _completeHandlers.Clear();
        }
    }
}
