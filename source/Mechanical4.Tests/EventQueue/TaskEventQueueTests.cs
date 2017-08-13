using System;
using System.Threading;
using NUnit.Framework;

namespace Mechanical4.EventQueue.Tests
{
    [TestFixture]
    public static class TaskEventQueueTests
    {
        private static readonly TimeSpan SleepTime = TimeSpan.FromMilliseconds(300);

        [Test]
        public static void EmptyingQueueDoesNotCloseItTest()
        {
            var queue = new TaskEventQueue();
            var subscriber = new TestEventHandler();
            queue.Subscribers.Add(subscriber);

            Thread.Sleep(SleepTime);
            Assert.True(queue.IsRunning);

            queue.Enqueue(new TestEvent());
            Thread.Sleep(SleepTime);
            Assert.NotNull(subscriber.LastEventHandled);
            Assert.True(queue.IsRunning);

            queue.BeginClose();
            Thread.Sleep(SleepTime);
            Assert.False(queue.IsRunning);
        }

        [Test]
        public static void NoEventsOrSubscribersAfterStoppingTest()
        {
            var queue = new TaskEventQueue();
            var subscriber = new TestEventHandler();
            queue.Subscribers.Add(subscriber);

            queue.BeginClose();
            Thread.Sleep(SleepTime);
            Assert.False(queue.IsRunning);

            queue.Enqueue(new TestEvent());
            Thread.Sleep(SleepTime);
            Assert.Null(subscriber.LastEventHandled);
            Assert.False(queue.Subscribers.Add(new TestEventHandler(), useWeakRef: false));
        }
    }
}
