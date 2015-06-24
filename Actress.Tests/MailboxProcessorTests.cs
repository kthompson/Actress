using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Actress.Tests
{
    /// <summary>
    /// https://github.com/fsharp/fsharp/blob/master/tests/xplat/core/FsharpUnitTestProject/SharedTests/control-mailbox.fs
    /// </summary>
    public class MailboxProcessorTests
    {
        private class Message
        {
        }

        private class Reset : Message
        {
        }

        private class Increment : Message
        {
            public Increment(int value)
            {
                this.Value = value;
            }

            public int Value { get; private set; }
        }

        private class Fetch : Message
        {
            public Fetch(IReplyChannel<int?> channel)
            {
                this.Channel = channel;
            }

            public IReplyChannel<int?> Channel { get; private set; }
        }

        private MailboxProcessor<Message> GetSimpleMailbox()
        {
            return new MailboxProcessor<Message>(async inbox =>
            {
                int n = 0;
                while (true)
                {
                    var msg = await inbox.Receive();

                    await Task.Delay(100);

                    if (msg is Increment)
                    {
                        n = n + ((Increment) msg).Value;
                    }
                    else if (msg is Reset)
                    {
                        n = 0;
                    }
                    else if (msg is Fetch)
                    {
                        var chan = ((Fetch) msg).Channel;
                        chan.Reply(n);
                    }
                }
            });
        }

        [Fact]
        public void DefaultTimeout()
        {
            // Given
            var mailbox = GetSimpleMailbox();

            // When
            mailbox.Start();

            Assert.Equal(Timeout.Infinite, mailbox.DefaultTimeout);

            mailbox.Post(new Reset());
            mailbox.Post(new Increment(1));

            var result = mailbox.TryPostAndReply<int?>(chan => new Fetch(chan));

            Assert.NotNull(result);
            Assert.Equal(1, result.Value);

            // Verify timeout when updating default timeout
            // We expect this to fail because of the 100ms sleep in the mailbox
            mailbox.DefaultTimeout = 10;
            mailbox.Post(new Reset());
            mailbox.Post(new Increment(1));

            result = mailbox.TryPostAndReply<int?>(chan => new Fetch(chan));
            Assert.Null(result);
        }

        [Fact]
        public void Receive_PostAndReply()
        {
            // Given
            var mb = MailboxProcessor.Start<IReplyChannel<int?>>(async inbox =>
            {
                var msg = await inbox.Receive();
                msg.Reply(100);
            });

            // When
            var result = mb.PostAndReply<int?>(channel => channel);

            Assert.Equal(100, result);
        }


        [Fact]
        public void MailboxProcessor_null()
        {
            // Given
            var mb = new MailboxProcessor<IReplyChannel<int?>>(async inbox => { });

            // When
            mb.Start();

            // Then
            // no exceptions
        }

        [Fact]
        public void Receive_PostAndReply2()
        {
            // Given
            var mb = MailboxProcessor.Start<IReplyChannel<int?>>(async inbox =>
            {
                var msg = await inbox.Receive();
                msg.Reply(100);

                var msg2 = await inbox.Receive();
                msg2.Reply(200);
            });

            // When
            var result1 = mb.PostAndReply<int?>(channel => channel);
            var result2 = mb.PostAndReply<int?>(channel => channel);


            // Then
            Assert.Equal(100, result1);
            Assert.Equal(200, result2);
        }

        [Fact]
        public void TryReceive_PostAndReply()
        {
            // Given
            var mb = MailboxProcessor.Start<IReplyChannel<int?>>(async inbox =>
            {
                var msg = await inbox.TryReceive();
                if (msg == null)
                {
                    Assert.True(false);
                    return;
                }

                msg.Reply(100);

                var msg2 = await inbox.TryReceive();
                if (msg2 == null)
                {
                    Assert.True(false);
                    return;
                }

                msg2.Reply(200);
            });

            // When
            var result1 = mb.PostAndReply<int?>(channel => channel);
            var result2 = mb.PostAndReply<int?>(channel => channel);


            // Then
            Assert.Equal(100, result1);
            Assert.Equal(200, result2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void TryReceive_timeout(int timeout)
        {
            // Given
            var mb = MailboxProcessor.Start<IReplyChannel<int?>>(async inbox =>
            {
                var replyChannel = await inbox.TryReceive();
                if (replyChannel == null)
                {
                    Assert.True(false);
                    return;
                }

                var msg2 = await inbox.TryReceive(timeout);
                replyChannel.Reply(msg2 == null ? 100 : 200);
            });

            // When
            var result1 = mb.PostAndReply<int?>(channel => channel);

            // Then
            Assert.Equal(100, result1);
        }

        [Theory]
        [InlineData(10)]
        public void TryReceivePostAndReply_With_Receive_timeout(int timeout)
        {
            // Given
            var mb = MailboxProcessor.Start<IReplyChannel<int?>>(async inbox =>
            {
                var replyChannel = await inbox.TryReceive();
                if (replyChannel == null)
                {
                    Assert.True(false);
                    return;
                }
                try
                {
                    var _ = await inbox.Receive(timeout);
                    Assert.True(false, "should have received timeout");
                }
                catch (TimeoutException)
                {
                    replyChannel.Reply(200);
                }
            });

            // When
            var result1 = mb.PostAndReply<int?>(channel => channel);

            // Then
            Assert.Equal(200, result1);
        }

        [Theory]
        [InlineData(10)]
        public void TryReceivePostAndReply_with_Scan_timeout(int timeout)
        {
            // Given
            var mb = MailboxProcessor.Start<IReplyChannel<int?>>(async inbox =>
            {
                var replyChannel = await inbox.TryReceive();
                if (replyChannel == null)
                {
                    Assert.True(false);
                    return;
                }
                try
                {
                    var _ = await inbox.Scan(__ =>
                    {
                        Assert.True(false, "Should have timedout");
                        return Task.FromResult(inbox);
                    }, timeout);
                    Assert.True(false, "should have received timeout");
                }
                catch (TimeoutException)
                {
                    replyChannel.Reply(200);
                }
            });

            // When
            var result1 = mb.PostAndReply<int?>(channel => channel);

            // Then
            Assert.Equal(200, result1);
        }

        [Theory]
        [InlineData(10)]
        public void TryReceivePostAndReply_with_TryScan_timeout(int timeout)
        {
            // Given
            var mb = MailboxProcessor.Start<IReplyChannel<int?>>(async inbox =>
            {
                var replyChannel = await inbox.TryReceive();
                if (replyChannel == null)
                {
                    Assert.True(false);
                    return;
                }
                var scanRes = await inbox.TryScan(__ =>
                {
                    Assert.True(false, "Should have timedout");
                    return Task.FromResult(inbox);
                }, timeout);

                if (scanRes == null)
                {
                    replyChannel.Reply(200);
                }
                else
                {
                    Assert.True(false, "TryScan should have failed");
                }

            });

            // When
            var result1 = mb.PostAndReply<int?>(channel => channel);

            // Then
            Assert.Equal(200, result1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(100000)]
        public void PostReceive(int n)
        {
            // Given
            var received = 0L;

            var mb = MailboxProcessor.Start<IOption<int>>(async inbox =>
            {
                for (var i = 0; i < n - 1; i++)
                {
                    var _ = await inbox.Receive();
                    Interlocked.Increment(ref received);
                }
            });


            // When
            for (var i = 0; i < n - 1; i++)
            {
                mb.Post(Option.Some(i));
            }

            // Then
            while (received < n)
            {
                var numReceived = Interlocked.Read(ref received);
                if (numReceived % 100 == 0)
                {
                    Trace.WriteLine(string.Format("received = {0}", numReceived));
                }
                Thread.Sleep(1);
            }

            Assert.Equal(n, received);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 100)]
        [InlineData(10, 0)]
        [InlineData(10, 1)]
        [InlineData(10, 100)]
        public void PostTryReceive(int timeout, int n)
        {
            // Given
            var received = 0L;

            var mb = MailboxProcessor.Start<IOption<int>>(async inbox =>
            {
                while (Interlocked.Read(ref received) < n)
                {
                    var msgOpt = await inbox.TryReceive(timeout);
                    if (msgOpt == null)
                    {
                        var numReceived = Interlocked.Read(ref received);
                        if (numReceived % 100 == 0)
                            Trace.WriteLine(string.Format("timeout!, received = {0}", numReceived));
                    }
                    else
                    {
                        Interlocked.Increment(ref received);
                    }
                }
            });


            // When
            for (var i = 0; i < n - 1; i++)
            {
                Thread.Sleep(1);
                mb.Post(Option.Some(i));
            }

            // Then
            while (Interlocked.Read(ref received) < n)
            {
                var numReceived = Interlocked.Read(ref received);
                if (numReceived%100 == 0)
                    Trace.WriteLine(string.Format("received = {0}", numReceived));

                Thread.Sleep(1);
            }

            Assert.Equal(n, received);
        }
    }
}