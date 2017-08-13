using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Mechanical4.EventQueue.Tests
{
    [TestFixture]
    public static class TaskEventQueueTests
    {
        private static readonly TimeSpan SleepTime = TimeSpan.FromMilliseconds(300);

        private static bool IsRunning( TaskEventQueue queue )
        {
            return queue.Task.Status == TaskStatus.Running;
        }

        [Test]
        public static void EmptyingQueueDoesNotCloseItTest()
        {
            var queue = new TaskEventQueue();
            var subscriber = new TestEventHandler();
            queue.Subscribers.Add(subscriber);

            Thread.Sleep(SleepTime);
            Assert.True(IsRunning(queue));

            queue.Enqueue(new TestEvent());
            Thread.Sleep(SleepTime);
            Assert.NotNull(subscriber.LastEventHandled);
            Assert.True(IsRunning(queue));

            queue.BeginClose();
            Thread.Sleep(SleepTime);
            Assert.False(IsRunning(queue));
        }

        [Test]
        public static void NoEventsOrSubscribersAfterStoppingTest()
        {
            var queue = new TaskEventQueue();
            var subscriber = new TestEventHandler();
            queue.Subscribers.Add(subscriber);

            queue.BeginClose();
            Thread.Sleep(SleepTime);
            Assert.False(IsRunning(queue));

            queue.Enqueue(new TestEvent());
            Thread.Sleep(SleepTime);
            Assert.Null(subscriber.LastEventHandled);
            Assert.False(queue.Subscribers.Add(new TestEventHandler(), useWeakRef: false));
        }
    }
}
