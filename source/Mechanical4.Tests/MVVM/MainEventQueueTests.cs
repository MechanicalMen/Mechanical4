using System;
using Mechanical4.EventQueue;
using Mechanical4.MVVM;
using Mechanical4.Tests.EventQueue;
using NUnit.Framework;

namespace Mechanical4.Tests.MVVM
{
    [TestFixture]
    public static class MainEventQueueTests
    {
        internal class TestCriticalEvent : TestEvent, ICriticalEvent { }

        [Test]
        public static void RegularEventHandlingWorksAsExpected()
        {
            var manualQueue = new ManualEventQueue();
            var app = new AppBaseTests.TestApp(manualQueue);

            var subscriber = new TestEventHandler();
            var evnt = new TestEvent();

            // use the main queue to subscribe and enqueue
            app.MainEventQueue.Subscribers.AddAll(subscriber);
            app.MainEventQueue.EnqueueRegular(evnt);

            // simulate wrapped queue handling events
            Assert.True(manualQueue.HandleNext());
            Assert.AreSame(evnt, subscriber.LastEventHandled);

            // shutdown works as expected
            app.MainEventQueue.BeginShutdown();
            while( manualQueue.HandleNext() ) ;
            Assert.True(manualQueue.IsShutDown);
        }

        [Test]
        public static void CriticalEventsAreHandledImmediately()
        {
            var manualQueue = new ManualEventQueue();
            var app = new AppBaseTests.TestApp(manualQueue);

            var subscriber = new TestEventHandler();
            app.MainEventQueue.Subscribers.AddAll(subscriber);

            var evnt = new TestCriticalEvent();
            app.MainEventQueue.HandleCritical(evnt); // also uses the same Subscribers as regular events

            // manualQueue is empty...
            Assert.False(manualQueue.HandleNext());

            // ... but the event was handled all the same
            Assert.AreSame(evnt, subscriber.LastEventHandled);
        }

        [Test]
        public static void RegularAndCriticalEventsMustUseCorrespondingMethod()
        {
            var manualQueue = new ManualEventQueue();
            var app = new AppBaseTests.TestApp(manualQueue);

            Assert.Throws<ArgumentException>(() => app.MainEventQueue.EnqueueRegular(new TestCriticalEvent()));
            Assert.Throws<ArgumentException>(() => app.MainEventQueue.HandleCritical(new TestEvent()));
        }

        [Test]
        public static void CriticalEventHandlingSuspendsRegularEventHandling() // also, critical event handling ignores this suspension, obviously
        {
            var manualQueue = new ManualEventQueue();
            var app = new AppBaseTests.TestApp(manualQueue);

            app.MainEventQueue.Subscribers.Add(
                DelegateEventHandler.On<TestCriticalEvent>(
                    evnt => Assert.True(app.MainEventQueue.EventHandling.IsSuspended)));

            Assert.False(app.MainEventQueue.EventHandling.IsSuspended);
            app.MainEventQueue.HandleCritical(new TestCriticalEvent());
            Assert.False(app.MainEventQueue.EventHandling.IsSuspended);
        }
    }
}
