using System;
using Mechanical4.EventQueue;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue
{
    [TestFixture]
    public static class ManualEventQueueTests
    {
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

            Assert.Null(evnt.EventEnqueuePos);
            Assert.True(queue.Enqueue(evnt));
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
            Assert.True(queue.Enqueue(evnt));
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
        public static void EventsCanNotBeEnqueuedAfterShuttingDown()
        {
            var queue = new ManualEventQueue();
            var testListener = new TestEventHandler();
            queue.Subscribers.AddAll(testListener);
            queue.Subscribers.Add(
                DelegateEventHandler.OnShuttingDown(
                    e => queue.Enqueue(new NamedEvent("shutting down event handler"))),
                weakRef: false);

            // enqueue shutting down event
            queue.BeginShutdown();

            // can enqueue, after the shutting down event is enqueued, but before it is handled
            Assert.True(queue.Enqueue(new TestEvent() { Value = 1 }));

            // handle shutting down event
            Assert.True(queue.HandleNext());
            Assert.Null(testListener.LastEventHandled);

            // try to enqueue another event, after the shutting down event is handled:
            // it should have no effect
            Assert.False(queue.Enqueue(new TestEvent() { Value = 2 }));

            // handle first test event
            Assert.True(queue.HandleNext());
            Assert.AreEqual(1, ((TestEvent)testListener.LastEventHandled).Value);

            // handle the event added in the shutting down event handler
            Assert.True(queue.HandleNext());
            Assert.IsInstanceOf<NamedEvent>(testListener.LastEventHandled); // false for null

            // handle shut down event
            Assert.True(queue.HandleNext(out var shutDownEvent));
            Assert.IsInstanceOf<ShutDownEvent>(shutDownEvent);

            // nothing more to handle
            Assert.True(queue.IsShutDown);
        }

        [Test]
        public static void IsShutDown()
        {
            // initially open
            var queue = new ManualEventQueue();
            Assert.False(queue.IsShutDown);

            // removing all subscribers does not shut down the queue
            queue.Subscribers.AddAll(new TestEventHandler(), weakRef: false);
            queue.Subscribers.Clear();
            Assert.False(queue.IsShutDown);

            // adding the shutting down event is not enough
            queue.BeginShutdown();
            Assert.False(queue.IsShutDown);

            // handling the shutting down event is not enough...
            queue.Subscribers.Add(
                DelegateEventHandler.OnShuttingDown(
                    e => queue.Enqueue(new NamedEvent("shutting down event handler"))),
                weakRef: false);
            Assert.True(queue.HandleNext());
            Assert.False(queue.IsShutDown);

            // handling the NamedEvent added in the ShuttingDownEvent handler, is not enough
            Assert.True(queue.HandleNext());
            Assert.False(queue.IsShutDown);

            // ... but handling the ShutDownEvent is
            Assert.True(queue.HandleNext());
            Assert.True(queue.IsShutDown);
            Assert.False(queue.HandleNext());
        }

        [Test]
        public static void NoNewSubscriptionsAfterShutdown()
        {
            var queue = new ManualEventQueue();
            var handler = new TestEventHandler();
            queue.Subscribers.AddAll(handler);

            queue.BeginShutdown();
            Assert.True(queue.HandleNext()); // shutting down event
            Assert.True(queue.HandleNext()); // shut down event
            Assert.True(queue.IsShutDown);
            Assert.False(queue.Subscribers.RemoveAll(handler)); // a shutdown queue has no subscribers
            Assert.False(queue.Subscribers.AddAll(new TestEventHandler(), weakRef: false));
        }

        [Test]
        public static void ShutdownRequest_Cancelled()
        {
            var queue = new ManualEventQueue();
            queue.Subscribers.Add(
                DelegateEventHandler.OnShutdownRequest(
                    e => e.Cancel = true),
                weakRef: false);

            // you can only add one request at a time
            Assert.True(queue.RequestShutdown());
            Assert.False(queue.RequestShutdown());

            // cancel the request
            Assert.True(queue.HandleNext());

            // no shutting down event
            Assert.False(queue.HandleNext());

            // you can add another request, once the previous one is out of the queue
            Assert.True(queue.RequestShutdown());
            Assert.True(queue.HandleNext());
            Assert.False(queue.HandleNext());
        }

        [Test]
        public static void ShutdownRequest_NotCancelled()
        {
            var queue = new ManualEventQueue();

            bool shuttingDownDetected = false;
            queue.Subscribers.Add(
                DelegateEventHandler.OnShuttingDown(
                    e => shuttingDownDetected = true),
                weakRef: false);

            // requests are granted by default (Cancel is false)
            Assert.True(queue.RequestShutdown());
            Assert.True(queue.HandleNext()); // request event
            Assert.False(shuttingDownDetected);
            Assert.True(queue.HandleNext()); // shutting down event
            Assert.True(shuttingDownDetected);
            Assert.True(queue.HandleNext()); // shut down event
            Assert.False(queue.HandleNext());
            Assert.True(queue.IsShutDown);
        }

        [Test]
        public static void ShutdownRequest_AfterShuttingDown()
        {
            // can not enqueue after shutting down event was enqueued
            var queue = new ManualEventQueue();
            Assert.True(queue.BeginShutdown());
            Assert.False(queue.Enqueue(new ShutdownRequestEvent()));

            // handling is skipped, if a shutting down event is enqueued beforehand
            queue = new ManualEventQueue();

            bool requestDetected = false;
            queue.Subscribers.Add(
                DelegateEventHandler.OnShutdownRequest(
                    e => requestDetected = true),
                weakRef: false);

            Assert.True(queue.Enqueue(new ShutdownRequestEvent())); // enqueueing succeeds
            Assert.True(queue.Enqueue(new ShuttingDownEvent())); // shutting down event added
            Assert.False(requestDetected);
            Assert.True(queue.HandleNext()); // the request was "handled" successfully
            Assert.False(requestDetected); // but the handler was not actually invoked

            // ensure there are no unexpected events in the queue, that could have shifted the position of the request event
            Assert.True(queue.HandleNext()); // shutting down event
            Assert.True(queue.HandleNext()); // shut down event
            Assert.False(queue.HandleNext());
            Assert.True(queue.IsShutDown);
        }

        [Test]
        public static void ShutdownRequest_IgnoredAfterShuttingDown()
        {
            var queue = new ManualEventQueue();
        }

        [Test]
        public static void HandleSupportsEventInheritance()
        {
            var queue = new ManualEventQueue();
            var subscriber = new AncestorEventHandler();
            queue.Subscribers.AddAll(subscriber);

            // subscriber does not handle events not in it's inheritance tree
            Assert.True(queue.Enqueue(new TestEvent()));
            Assert.True(queue.HandleNext());
            Assert.Null(subscriber.LastEventHandled);

            // subscriber accepts event type it explicitly stated it would handle
            var namedEvent = new NamedEvent("name");
            Assert.True(queue.Enqueue(namedEvent));
            Assert.True(queue.HandleNext());
            Assert.AreSame(namedEvent, subscriber.LastEventHandled);

            // subscriber accepts event that inherit what it declared it would handle
            var derivedEvent = new DescendantEvent();
            Assert.True(queue.Enqueue(derivedEvent));
            Assert.True(queue.HandleNext());
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
            Assert.False(queue.Enqueue(evnt));
            Assert.False(queue.HandleNext());
            Assert.Null(testListener.LastEventHandled);

            queue.EventAdding.Resume();
            Assert.True(queue.Enqueue(evnt));
            Assert.True(queue.HandleNext());
            Assert.AreSame(evnt, testListener.LastEventHandled);
        }

        [Test]
        public static void EnqueueReturnValue()
        {
            var queue = new ManualEventQueue();
            var evnt = new TestEvent();

            Assert.True(queue.Enqueue(evnt));

            // adding the same event a second time
            Assert.False(queue.Enqueue(evnt));

            // handling it and adding it again is fine however
            Assert.True(queue.HandleNext());
            Assert.False(queue.HandleNext()); // verify that we only added it once
            Assert.True(queue.Enqueue(evnt));

            // adding suspension
            queue.EventAdding.Suspend();
            Assert.False(queue.Enqueue(new TestEvent()));
            queue.EventAdding.Resume();
            Assert.True(queue.Enqueue(new TestEvent()));

            // handle all events
            while( queue.HandleNext() ) ;

            // add and handle shutting down event
            Assert.True(queue.Enqueue(new ShuttingDownEvent()));
            Assert.False(queue.Enqueue(new ShuttingDownEvent())); // another shutting down event is already enqueued
            Assert.True(queue.HandleNext());
            Assert.False(queue.Enqueue(new TestEvent())); // no more events of any kind can be added
        }

        [Test]
        public static void EventHandlerExceptions()
        {
            var queue = new ManualEventQueue();

            queue.Subscribers.Add(
                DelegateEventHandler.On<TestEvent>(
                    e => throw new ArgumentOutOfRangeException()),
                weakRef: false);

            queue.Subscribers.Add(
                DelegateEventHandler.On<TestEvent>(
                    e => throw new MissingMemberException()),
                weakRef: false);

            bool argumentExceptionFound = false;
            bool memberExceptionFound = false;
            bool unhandledExceptionEventHandled = false;
            queue.Subscribers.Add(
                DelegateEventHandler.On<UnhandledExceptionEvent>(
                    e =>
                    {
                        unhandledExceptionEventHandled = true;

                        if( e.Type.IndexOf(nameof(ArgumentOutOfRangeException), StringComparison.Ordinal) != -1 )
                            argumentExceptionFound = true;
                        else if( e.Type.IndexOf(nameof(MissingMemberException), StringComparison.Ordinal) != -1 )
                            memberExceptionFound = true;
                    }),
                weakRef: false);

            // unhandled exception events are generated by default
            queue.Enqueue(new TestEvent());
            Assert.False(argumentExceptionFound);
            Assert.False(memberExceptionFound);
            Assert.False(unhandledExceptionEventHandled);
            Assert.True(queue.HandleNext()); // test event
            Assert.True(queue.HandleNext()); // argument exception
            Assert.True(queue.HandleNext()); // missing member exception
            Assert.True(argumentExceptionFound);
            Assert.True(memberExceptionFound);
            Assert.True(unhandledExceptionEventHandled);

            // unhandled exception event suppression
            queue.Enqueue(new TestEvent());
            argumentExceptionFound = false;
            memberExceptionFound = false;
            unhandledExceptionEventHandled = false;
            queue.RaiseUnhandledEvents.Suspend();
            Assert.True(queue.HandleNext());
            Assert.False(queue.HandleNext());
            queue.RaiseUnhandledEvents.Resume();
            Assert.False(argumentExceptionFound);
            Assert.False(memberExceptionFound);
            Assert.False(unhandledExceptionEventHandled);

            // event addition suppression
            queue.Enqueue(new TestEvent());
            queue.EventAdding.Suspend();
            Assert.True(queue.HandleNext());
            Assert.False(queue.HandleNext());
            queue.EventAdding.Resume();
            Assert.False(argumentExceptionFound);
            Assert.False(memberExceptionFound);
            Assert.False(unhandledExceptionEventHandled);
        }
    }
}
