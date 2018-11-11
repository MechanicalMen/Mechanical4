using System;
using Mechanical4.EventQueue;
using Mechanical4.EventQueue.Events;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue
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

        internal class DescendantEvent : NamedEvent
        {
            internal DescendantEvent()
                : base("test")
            {
            }
        }

        internal class AncestorEventHandler : IEventHandler<NamedEvent>
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

            queue.Enqueue(evnt);

            Assert.True(evnt.EventEnqueuePos.Contains(nameof(EnqueueUpdatesMetadata)));
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
            queue.Subscribers.AddAll(subscriber);
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
            queue.Subscribers.AddAll(testListener);
            queue.Subscribers.AddAll(new EnqueuesEventOnClosing(queue), weakRef: false);

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
            queue.Subscribers.AddAll(new TestEventHandler(), weakRef: false);
            queue.Subscribers.Clear();
            Assert.False(queue.IsClosed);

            // adding the closing event is not enough
            queue.BeginClose();
            Assert.False(queue.IsClosed);

            // handling the closing event is not enough...
            queue.Subscribers.AddAll(new EnqueuesEventOnClosing(queue), weakRef: false);
            Assert.True(queue.HandleNext());
            Assert.False(queue.IsClosed);

            // ... but clearing the queue afterwards is
            Assert.True(queue.HandleNext());
            Assert.True(queue.IsClosed);
            Assert.False(queue.HandleNext());
        }

        [Test]
        public static void NoNewSubscriptionsAfterClosed()
        {
            var queue = new ManualEventQueue();
            var handler = new TestEventHandler();
            queue.Subscribers.AddAll(handler);

            queue.BeginClose();
            queue.HandleNext();
            Assert.True(queue.IsClosed);
            Assert.False(queue.Subscribers.RemoveAll(handler)); // closing the queue removes subscribers
            Assert.False(queue.Subscribers.AddAll(new TestEventHandler(), weakRef: false));
        }

        [Test]
        public static void HandleSupportsEventInheritance()
        {
            var queue = new ManualEventQueue();
            var subscriber = new AncestorEventHandler();
            queue.Subscribers.AddAll(subscriber);

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
        }

        [Test]
        public static void EventHandlingSuspension()
        {
            var queue = new ManualEventQueue();
            Assert.False(queue.EventHandling.IsSuspended);

            var testListener = new TestEventHandler();
            queue.Subscribers.AddAll(testListener);
            var evnt = new TestEvent();
            queue.Enqueue(evnt);

            queue.EventHandling.Suspend();
            Assert.False(queue.HandleNext());
            Assert.Null(testListener.LastEventHandled);

            queue.EventHandling.Resume();
            Assert.True(queue.HandleNext());
            Assert.AreSame(evnt, testListener.LastEventHandled);
        }

        [Test]
        public static void EventAddingSuspension()
        {
            var queue = new ManualEventQueue();
            Assert.False(queue.EventHandling.IsSuspended);

            var testListener = new TestEventHandler();
            queue.Subscribers.AddAll(testListener);
            var evnt = new TestEvent();

            queue.EventAdding.Suspend();
            queue.Enqueue(evnt);
            Assert.False(queue.HandleNext());
            Assert.Null(testListener.LastEventHandled);

            queue.EventAdding.Resume();
            queue.Enqueue(evnt);
            Assert.True(queue.HandleNext());
            Assert.AreSame(evnt, testListener.LastEventHandled);
        }
    }
}
