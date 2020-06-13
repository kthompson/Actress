namespace Actress
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class MailboxProcessor
    {
        public static MailboxProcessor<T> Start<T>(Func<MailboxProcessor<T>, Task> body, CancellationToken? cancellationToken = null)
            where T : class
        {
            var mailboxProcessor = new MailboxProcessor<T>(body, cancellationToken);
            mailboxProcessor.Start();
            return mailboxProcessor;
        }
    }

    public class MailboxProcessor<TMsg> : IDisposable
    {
        private readonly Func<MailboxProcessor<TMsg>, Task> _body;
        private readonly CancellationToken _cancellationToken;
        private readonly Mailbox<TMsg> _mailbox;
        private bool _started;
        private readonly Observable<Exception> _errorEvent;

        public MailboxProcessor(Func<MailboxProcessor<TMsg>, Task> body, CancellationToken? cancellationToken = null)
        {
            this._body = body;
            this._cancellationToken = cancellationToken ?? Task.Factory.CancellationToken;
            this._mailbox = new Mailbox<TMsg>();
            this.DefaultTimeout = Timeout.Infinite;
            this._started = false;
            this._errorEvent = new Observable<Exception>();
        }

        public IObservable<Exception> Errors => this._errorEvent;

        public int CurrentQueueLength => this._mailbox.CurrentQueueLength;

        public int DefaultTimeout { get; set; }

        public void Start()
        {
            if (this._started)
                throw new InvalidOperationException("MailboxProcessor already started");

            this._started = true;

            // Protect the execution and send errors to the event.
            // Note that exception stack traces are lost in this design - in an extended design
            // the event could propagate an ExceptionDispatchInfo instead of an Exception.

            async Task StartAsync()
            {
                try
                {
                    await _body(this);
                }
                catch (Exception exception)
                {
                    _errorEvent.OnNext(exception);
                    throw;
                }
            }

            Task.Run(StartAsync, this._cancellationToken);
        }

        public void Post(TMsg message)
        {
            this._mailbox.Post(message);
        }

        public TReply TryPostAndReply<TReply>(Func<IReplyChannel<TReply>, TMsg> msgf, int? timeout = null)
        {
            var tcs = new TaskCompletionSource<TReply>();
            var msg = msgf(new ReplyChannel<TReply>(reply =>
            {
                tcs.SetResult(reply);
            }));

            this._mailbox.Post(msg);

            var task = tcs.Task;

            if (task.Wait(timeout ?? this.DefaultTimeout))
                return task.Result;

            return default(TReply);
        }

        public TReply PostAndReply<TReply>(Func<IReplyChannel<TReply>, TMsg> buildMessage, int? timeout = null)
        {
            var res = TryPostAndReply(buildMessage, timeout);
            if (!Equals(res, default(TReply)))
                return res;

            throw new TimeoutException("MailboxProcessor PostAndReply timed out");
        }

        public Task<TReply> PostAndTryAsyncReply<TReply>(Func<IReplyChannel<TReply>, TMsg> msgf, int? timeout = null)
        {
            timeout = timeout ?? this.DefaultTimeout;
            var tcs = new TaskCompletionSource<TReply>();
            var msg = msgf(new ReplyChannel<TReply>(reply =>
            {
                tcs.SetResult(reply);
            }));

            this._mailbox.Post(msg);

            var task = tcs.Task;

            if (timeout == Timeout.Infinite)
                return tcs.Task;

            if (task.Wait(timeout.Value))
                return task;

            return Task.FromResult<TReply>(default(TReply));
        }

        public async Task<TReply> PostAndAsyncReply<TReply>(Func<IReplyChannel<TReply>, TMsg> msgf, int? timeout = null)
        {
            var res = await PostAndTryAsyncReply(msgf, timeout);
            if (!Equals(res, default(TReply)))
                return res;

            throw new TimeoutException($"MailboxProcessor PostAndAsyncReply timed out");
        }

        public Task<TMsg> Receive(int? timeout = null)
        {
            return this._mailbox.Receive(timeout ?? this.DefaultTimeout);
        }

        public Task<TMsg> TryReceive(int? timeout = null)
        {
            return this._mailbox.TryReceive(timeout ?? this.DefaultTimeout);
        }

        public Task<T> Scan<T>(Func<TMsg, Task<T>> f, int? timeout = null) where T : class
        {
            return this._mailbox.Scan(f, timeout ?? this.DefaultTimeout);
        }

        public Task<T> TryScan<T>(Func<TMsg, Task<T>> f, int? timeout = null) where T : class
        {
            return this._mailbox.TryScan(f, timeout ?? this.DefaultTimeout);
        }

        public void Dispose()
        {
            this._mailbox.Dispose();
        }
    }
}