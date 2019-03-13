using System;
using Mechanical4.EventQueue;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue
{
    [TestFixture]
    public static class DelegateEventHandlerTests
    {
        [Test]
        public static void StatelessHandler()
        {
            var queue = new ManualEventQueue();

            // create subscriber
            TestEvent lastEventHandled = null;
            var subscriber = DelegateEventHandler.On<TestEvent>(evnt => lastEventHandled = evnt);
            queue.Subscribers.Add(subscriber);

            // enqueue and handle event
            queue.Enqueue(new TestEvent() { Value = 3 });
            Assert.Null(lastEventHandled);
            Assert.True(queue.HandleNext());

            // test subscriber
            Assert.NotNull(lastEventHandled);
            Assert.AreEqual(3, lastEventHandled.Value);

            // queue is empty again (no unhandled exceptions or anything else)
            Assert.False(queue.HandleNext());
        }

        [Test]
        public static void StatefulHandler()
        {
            var queue = new ManualEventQueue();

            // create subscriber
            TestEvent lastEventHandled = null;
            var subscriber = DelegateEventHandler.On(
                ( int currentState, TestEvent evnt ) =>
                {
                    lastEventHandled = evnt;
                    evnt.Value += currentState;
                    return currentState + 1;
                },
                initialState: 10);
            queue.Subscribers.Add(subscriber);

            // enqueue events
            queue.Enqueue(new TestEvent() { Value = 3 });
            queue.Enqueue(new TestEvent() { Value = 4 });
            Assert.Null(lastEventHandled);

            // handle first event
            Assert.True(queue.HandleNext());
            Assert.NotNull(lastEventHandled);
            Assert.AreEqual(13, lastEventHandled.Value);

            // handle second event
            Assert.True(queue.HandleNext());
            Assert.NotNull(lastEventHandled);
            Assert.AreEqual(15, lastEventHandled.Value);

            // queue is empty again (no unhandled exceptions or anything else)
            Assert.False(queue.HandleNext());
        }

        [Test]
        public static void ExceptionHandler()
        {
            var queue = new ManualEventQueue();

            // create subscriber
            UnhandledExceptionEvent lastEventHandled = null;
            var subscriber = DelegateEventHandler.OnException(evnt => lastEventHandled = evnt);
            queue.Subscribers.Add(subscriber);

            // enqueue and handle event
            queue.Enqueue(new UnhandledExceptionEvent(new UnauthorizedAccessException()));
            Assert.Null(lastEventHandled);
            Assert.True(queue.HandleNext());

            // test subscriber
            Assert.NotNull(lastEventHandled);
            Test.OrdinalEquals(lastEventHandled.Type, typeof(UnauthorizedAccessException).ToString());

            // queue is empty again (no unhandled exceptions or anything else)
            Assert.False(queue.HandleNext());
        }

        [Test]
        public static void ShutdownRequestHandler()
        {
            var queue = new ManualEventQueue();

            // create subscriber
            ShutdownRequestEvent lastEventHandled = null;
            var subscriber = DelegateEventHandler.OnShutdownRequest(evnt => lastEventHandled = evnt);
            queue.Subscribers.Add(subscriber);

            // enqueue and handle event
            queue.RequestShutdown();
            Assert.Null(lastEventHandled);
            Assert.True(queue.HandleNext());

            // test subscriber
            Assert.NotNull(lastEventHandled);
            Assert.True(queue.HandleNext()); // shutting down event
            Assert.True(queue.HandleNext()); // shut down event
            Assert.True(queue.IsShutDown);
        }

        [Test]
        public static void ShuttingDownHandler()
        {
            var queue = new ManualEventQueue();

            // create subscriber
            ShuttingDownEvent lastEventHandled = null;
            var subscriber = DelegateEventHandler.OnShuttingDown(evnt => lastEventHandled = evnt);
            queue.Subscribers.Add(subscriber);

            // enqueue and handle event
            queue.BeginShutdown();
            Assert.Null(lastEventHandled);
            Assert.True(queue.HandleNext()); // shutting down event

            // test subscriber
            Assert.NotNull(lastEventHandled);
            Assert.True(queue.HandleNext()); // shut down event
            Assert.True(queue.IsShutDown);
        }
    }
}
