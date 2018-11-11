using System;
using System.Threading;
using System.Threading.Tasks;
using Mechanical4.EventQueue;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue
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
        public static void EmptyingQueueDoesNotCloseIt()
        {
            var queue = new TaskEventQueue();
            var subscriber = new TestEventHandler();
            queue.Subscribers.AddAll(subscriber);

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
        public static void NoEventsOrSubscribersAfterStopping()
        {
            var queue = new TaskEventQueue();
            var subscriber = new TestEventHandler();
            queue.Subscribers.AddAll(subscriber);

            queue.BeginClose();
            Thread.Sleep(SleepTime);
            Assert.False(IsRunning(queue));

            queue.Enqueue(new TestEvent());
            Thread.Sleep(SleepTime);
            Assert.Null(subscriber.LastEventHandled);
            Assert.False(queue.Subscribers.AddAll(new TestEventHandler(), weakRef: false));
        }

        [Test]
        public static void EventHandlingSuspension()
        {
            var queue = new TaskEventQueue();
            Assert.False(queue.EventHandling.IsSuspended);

            var subscriber = new TestEventHandler();
            queue.Subscribers.AddAll(subscriber);
            var evnt = new TestEvent();

            queue.EventHandling.Suspend();
            queue.Enqueue(evnt);
            Thread.Sleep(SleepTime);
            Assert.Null(subscriber.LastEventHandled);

            queue.EventHandling.Resume();
            Thread.Sleep(SleepTime);
            Assert.AreSame(evnt, subscriber.LastEventHandled);

            queue.BeginClose();
        }
    }
}
