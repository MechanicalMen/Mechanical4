using System;
using Mechanical4.EventQueue;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue
{
    [TestFixture]
    public static class MainEventQueueTests
    {
        #region SuspensionTestingEventHandler

        private class SuspensionTestingEventHandler : TestEventHandler
        {
            private readonly IEventQueue eventQueue;

            internal SuspensionTestingEventHandler( IEventQueue eq )
            {
                this.eventQueue = eq;
            }

            public override void Handle( TestEvent evnt )
            {
                Assert.True(this.eventQueue.IsSuspended);

                base.Handle(evnt);
            }
        }

        #endregion

        #region TestCriticalEventHandler

        private class TestCriticalEventHandler : TestEventHandler
        {
            //// NOTE: sets the Value of handled events
            //// NOTE: optionally enqueues a second critical event, while handling the first one

            private readonly IEventQueue eventQueue;

            internal TestCriticalEventHandler( IEventQueue eq )
            {
                this.eventQueue = eq;
            }

            public override void Handle( TestEvent evnt )
            {
                base.Handle(evnt);
                evnt.Value = int.MinValue;

                var e = this.EventToEnqueue;
                this.EventToEnqueue = null;

                if( e != null )
                    this.eventQueue.Enqueue(e);
            }

            public TestCriticalEvent EventToEnqueue { get; set; }
        }

        #endregion

        [Test]
        public static void BasicTests()
        {
            var manualQueue = new ManualEventQueue();

            Assert.Throws<ArgumentNullException>(() => new MainEventQueue(null));
            var mainQueue = new MainEventQueue(manualQueue);

            Assert.AreSame(mainQueue.Subscribers, manualQueue.Subscribers);

            Assert.Throws<NotSupportedException>(() => ((IEventQueue)mainQueue).IsSuspended.ToString());
            Assert.Throws<NotSupportedException>(() => { ((IEventQueue)mainQueue).IsSuspended = false; });
        }

        [Test]
        public static void RegularEventHandlingWorksAsExpected()
        {
            var manualQueue = new ManualEventQueue();
            var mainQueue = new MainEventQueue(manualQueue);

            var subscriber = new TestEventHandler();
            var evnt = new TestEvent();

            // use the main queue to subscribe and enqueue
            mainQueue.Subscribers.Add(subscriber);
            mainQueue.Enqueue(evnt);

            // simulate wrapped queue handling events
            Assert.True(manualQueue.HandleNext());
            Assert.AreSame(evnt, subscriber.LastEventHandled);

            // closing works as expected
            mainQueue.BeginClose();
            Assert.True(manualQueue.HandleNext());
            Assert.True(manualQueue.IsClosed);
        }

        [Test]
        public static void EnqueueDirectlyHandlesCriticalEvents()
        {
            var manualQueue = new ManualEventQueue();
            var mainQueue = new MainEventQueue(manualQueue);

            var subscriber = new TestEventHandler(); // regular TestEvent subscriber
            mainQueue.Subscribers.Add(subscriber);

            // Enqueue implicitly handles critical events, without calling the wrapped queue
            var evnt = new TestCriticalEvent();
            mainQueue.Enqueue(evnt);
            Assert.False(manualQueue.HandleNext());
            Assert.AreSame(evnt, subscriber.LastEventHandled);
        }

        [Test]
        public static void CriticalEventsSuspendWrappedQueue()
        {
            var manualQueue = new ManualEventQueue();
            var mainQueue = new MainEventQueue(manualQueue);

            var subscriber = new SuspensionTestingEventHandler(manualQueue);
            mainQueue.Subscribers.Add(subscriber);

            mainQueue.Enqueue(new TestCriticalEvent());
            Assert.NotNull(subscriber.LastEventHandled);
        }

        [Test]
        public static void CriticalEventsCanRaiseCriticalEvents()
        {
            var manualQueue = new ManualEventQueue();
            var mainQueue = new MainEventQueue(manualQueue);
            var subscriber = new TestCriticalEventHandler(mainQueue);
            mainQueue.Subscribers.Add(subscriber);

            // enqueue a critical event, and prepare a second one to be enqueued, while the first one is being handled
            var evnt1 = new TestCriticalEvent() { Value = 4 };
            var evnt2 = new TestCriticalEvent() { Value = 5 };
            subscriber.EventToEnqueue = evnt2;
            mainQueue.Enqueue(evnt1);

            // both critical events were handled
            Assert.False(manualQueue.HandleNext());
            Assert.AreEqual(int.MinValue, evnt1.Value);
            Assert.AreEqual(int.MinValue, evnt2.Value);
            Assert.AreSame(evnt2, subscriber.LastEventHandled);
        }
    }
}
