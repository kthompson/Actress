using System;

namespace Actress
{
    public interface IReplyChannel<TReply>
    {
        void Reply(TReply reply);
    }

    class ReplyChannel<TReply> : IReplyChannel<TReply>
    {
        private readonly Action<TReply> _replyf;

        internal ReplyChannel(Action<TReply> replyf)
        {
            _replyf = replyf;
        }

        public void Reply(TReply reply)
        {
            _replyf(reply);
        }
    }
}