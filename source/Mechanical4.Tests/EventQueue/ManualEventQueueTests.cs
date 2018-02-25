using System;
using Mechanical4.EventQueue.Events;
using NUnit.Framework;

namespace Mechanical4.EventQueue.Tests
{
    [TestFixture]
    public static class ManualEventQueueTests
    {
        #region EnqueuesEventOnClosing

        private class EnqueuesEventOnClosing : IEventHandler<EventQueueClosingEvent>
        {
            private readonly IEventQueue queue;

            internal EnqueuesEventOnClosing( IEventQueue eq )
            {
                this.queue = eq ?? throw new ArgumentNullException(nameof(eq));
            }

            public void Handle( EventQueueClosingEvent evnt )
            {
                this.queue.Enqueue(new NamedEvent("closing event handler"));
            }
        }

        #endregion

        #region DescendantEvent, NamedEventHandler

        private class DescendantEvent : NamedEvent
        {
            internal DescendantEvent()
                : base("test")
            {
            }
        }

        private class AncestorEventHandler : IEventHandler<NamedEvent>
        {
            public EventBase LastEventHandled { get; private set; }

            public void Handle( NamedEvent evnt )
            {
                this.LastEventHandled = evnt;
            }
        }

        #endregion

        [Test]
        public static void EnqueueUpdatesMetadata()
        {
            var queue = new ManualEventQueue();
            var evnt = new NamedEvent("test");

            Assert.AreEqual(null, evnt.EventEnqueuePos);
            Assert.AreEqual(DateTime.MinValue, evnt.EventEnqueueTime);
            Assert.AreEqual(DateTimeKind.Unspecified, evnt.EventEnqueueTime.Date.Kind);

            queue.Enqueue(evnt);

            Assert.True(evnt.EventEnqueuePos.Contains(nameof(EnqueueUpdatesMetadata)));
            Assert.AreEqual(DateTime.UtcNow.Date, evnt.EventEnqueueTime.Date);
            Assert.AreEqual(DateTimeKind.Utc, evnt.EventEnqueueTime.Date.Kind);
        }

        [Test]
        public static void HandleNextInvokesHandler()
        {
            var queue = new ManualEventQueue();
            var evnt = new TestEvent();
            var subscriber = new TestEventHandler();

            // queue empty
            Assert.False(queue.HandleNext());

            // enqueueing does not invoke handlers
            queue.Subscribers.Add(subscriber);
            queue.Enqueue(evnt);
            Assert.Null(subscriber.LastEventHandled);

            // invoking handlers is successful
            Assert.True(queue.HandleNext());
            Assert.AreSame(evnt, subscriber.LastEventHandled);

            // queue is empty again
            Assert.False(queue.HandleNext());
        }

        [Test]
        public static void EventsCanNotBeEnqueuedTwice()
        {
            var queue = new ManualEventQueue();
            var evnt = new TestEvent();

            // the same instance twice won't work
            queue.Enqueue(evnt);
            queue.Enqueue(evnt);
            Assert.True(queue.HandleNext());
            Assert.False(queue.HandleNext());

            // .. but two separate instances of the same event are fine
            queue.Enqueue(new TestEvent());
            queue.Enqueue(new TestEvent());
            Assert.True(queue.HandleNext());
            Assert.True(queue.HandleNext());
        }

        [Test]
        public static void EventsCanNotBeEnqueuedAfterClosing()
        {
            var queue = new ManualEventQueue();
            var testListener = new TestEventHandler();
            queue.Subscribers.Add(testListener);
            queue.Subscribers.Add(new EnqueuesEventOnClosing(queue), useWeakRef: false);

            // enqueue closing event
            queue.BeginClose();

            // can enqueue, after closing is enqueued, but before it is handled
            queue.Enqueue(new TestEvent() { Value = 1 });

            // handle closing event
            Assert.True(queue.HandleNext());
            Assert.Null(testListener.LastEventHandled);

            // try to enqueue another event, after the closing event is handled:
            // it should have no effect
            queue.Enqueue(new TestEvent() { Value = 2 });

            // handle first test event
            Assert.True(queue.HandleNext());
            Assert.AreEqual(1, ((TestEvent)testListener.LastEventHandled).Value);

            // handle the event added in the closing handler
            Assert.True(queue.HandleNext());
            Assert.IsInstanceOf<NamedEvent>(testListener.LastEventHandled); // false for null

            // nothing more to handle
            Assert.True(queue.IsClosed);
        }

        [Test]
        public static void IsClosed()
        {
            // initially open
            var queue = new ManualEventQueue();
            Assert.False(queue.IsClosed);

            // removing all subscribers does not close the queue
            queue.Subscribers.Add(new TestEventHandler(), useWeakRef: false);
            queue.Subscribers.Clear();
            Assert.False(queue.IsClosed);

            // adding the closing event is not enough
            queue.BeginClose();
            Assert.False(queue.IsClosed);

            // handling the closing event is not enough...
            queue.Subscribers.Add(new EnqueuesEventOnClosing(queue), useWeakRef: false);
            Assert.True(queue.HandleNext());
            Assert.False(queue.IsClosed);

            // ... but clearing the queue afterwards is
            Assert.True(queue.HandleNext());
            Assert.True(queue.IsClosed);
            Assert.False(queue.HandleNext());
        }

        [Test]
        public static void CriticalEventsHandledBeforeOthers()
        {
            var queue = new ManualEventQueue();
            var subscriber = new TestEventHandler();
            queue.Subscribers.Add(subscriber);

            queue.Enqueue(new TestEvent() { Value = 3 }, critical: false);
            queue.Enqueue(new TestEvent() { Value = 4 }, critical: true);
            queue.Enqueue(new TestEvent() { Value = 5 }, critical: false);
            queue.Enqueue(new TestEvent() { Value = 6 }, critical: true);
            Assert.True(queue.HandleNext());
            Assert.AreEqual(4, ((TestEvent)subscriber.LastEventHandled).Value);
            Assert.True(queue.HandleNext());
            Assert.AreEqual(6, ((TestEvent)subscriber.LastEventHandled).Value);
            Assert.True(queue.HandleNext());
            Assert.AreEqual(3, ((TestEvent)subscriber.LastEventHandled).Value);
            Assert.True(queue.HandleNext());
            Assert.AreEqual(5, ((TestEvent)subscriber.LastEventHandled).Value);
        }

        [Test]
        public static void CriticalClosingEvent()
        {
            var queue = new ManualEventQueue();
            var subscriber = new TestEventHandler();
            queue.Subscribers.Add(subscriber);

            queue.Enqueue(new TestEvent() { Value = 3 }, critical: false);
            queue.Enqueue(new TestEvent() { Value = 4 }, critical: true);
            queue.BeginClose(critical: true);
            Assert.True(queue.HandleNext());
            Assert.AreEqual(4, ((TestEvent)subscriber.LastEventHandled).Value); // critical events handled in FIFO order
            Assert.True(queue.HandleNext());
            Assert.True(queue.IsClosed);
            Assert.AreEqual(4, ((TestEvent)subscriber.LastEventHandled).Value); // #3 never handled!
        }

        [Test]
        public static void NoNewSubscriptionsAfterClosed()
        {
            var queue = new ManualEventQueue();
            queue.Subscribers.Add(new TestEventHandler(), useWeakRef: false);

            queue.BeginClose();
            queue.HandleNext();
            Assert.True(queue.IsClosed);
            Assert.False(queue.Subscribers.Remove<TestEvent>()); // closing the queue removes subscribers
            Assert.False(queue.Subscribers.Add(new TestEventHandler(), useWeakRef: false));
        }

        [Test]
        public static void HandleAndRemoveAcceptTypeInheritance()
        {
            var queue = new ManualEventQueue();
            var subscriber = new AncestorEventHandler();
            queue.Subscribers.Add(subscriber);

            // subscriber does not handle events not in it's inheritance tree
            queue.Enqueue(new TestEvent());
            queue.HandleNext();
            Assert.Null(subscriber.LastEventHandled);

            // subscriber accepts event type it explicitly stated it would handle
            var namedEvent = new NamedEvent("name");
            queue.Enqueue(namedEvent);
            queue.HandleNext();
            Assert.AreSame(namedEvent, subscriber.LastEventHandled);

            // subscriber accepts event that inherit what it declared it would handle
            var derivedEvent = new DescendantEvent();
            queue.Enqueue(derivedEvent);
            queue.HandleNext();
            Assert.AreSame(derivedEvent, subscriber.LastEventHandled);

            // Remove<T> preserves subscribers, if they can not handle the event
            Assert.False(queue.Subscribers.Remove<TestEvent>());
            Assert.False(queue.Subscribers.Remove<EventBase>());

            // Remove<T> triggers on event type equality
            Assert.True(queue.Subscribers.Remove<NamedEvent>());
            Assert.False(queue.Subscribers.Remove<NamedEvent>());

            // Remove<T> triggers on event type inheritance
            queue.Subscribers.Add(subscriber);
            Assert.True(queue.Subscribers.Remove<DescendantEvent>());
            Assert.False(queue.Subscribers.Remove<DescendantEvent>());
        }

        [Test]
        public static void SuspensionDisablesHandling()
        {
            var queue = new ManualEventQueue();
            Assert.False(queue.IsSuspended);

            var testListener = new TestEventHandler();
            queue.Subscribers.Add(testListener);
            var evnt = new TestEvent();
            queue.Enqueue(evnt);

            queue.IsSuspended = true;
            Assert.False(queue.HandleNext());
            Assert.Null(testListener.LastEventHandled);

            queue.IsSuspended = false;
            Assert.True(queue.HandleNext());
            Assert.AreSame(evnt, testListener.LastEventHandled);
        }
    }
}
