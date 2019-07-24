using System;
using Mechanical4.EventQueue;
using Mechanical4.MVVM;
using Mechanical4.Tests.EventQueue;
using NUnit.Framework;

namespace Mechanical4.Tests.MVVM
{
    internal static class MainEventQueueTests
    {
        internal class TestCriticalEvent : TestEvent, ICriticalEvent { }

        internal static void RegularEventHandlingWorksAsExpected()
        {
            var subscriber = new TestEventHandler();
            var evnt = new TestEvent();

            // use the main queue to subscribe and enqueue
            AppBaseTests.TestApp.MainEventQueue.Subscribers.AddAll(subscriber);
            AppBaseTests.TestApp.MainEventQueue.EnqueueRegular(evnt);

            // simulate wrapped queue handling events
            var manualQueue = (ManualEventQueue)AppBaseTests.TestApp.MainEventQueue.BaseEventQueue;
            Assert.True(manualQueue.HandleNext());
            Assert.AreSame(evnt, subscriber.LastEventHandled);
        }

        internal static void CriticalEventsAreHandledImmediately()
        {
            var subscriber = new TestEventHandler();
            AppBaseTests.TestApp.MainEventQueue.Subscribers.AddAll(subscriber);

            var evnt = new TestCriticalEvent();
            AppBaseTests.TestApp.MainEventQueue.HandleCritical(evnt); // also uses the same Subscribers as regular events

            // manualQueue is empty...
            var manualQueue = (ManualEventQueue)AppBaseTests.TestApp.MainEventQueue.BaseEventQueue;
            Assert.False(manualQueue.HandleNext());

            // ... but the event was handled all the same
            Assert.AreSame(evnt, subscriber.LastEventHandled);
        }

        internal static void RegularAndCriticalEventsMustUseCorrespondingMethod()
        {
            Assert.Throws<ArgumentException>(() => AppBaseTests.TestApp.MainEventQueue.EnqueueRegular(new TestCriticalEvent()));
            Assert.Throws<ArgumentException>(() => AppBaseTests.TestApp.MainEventQueue.HandleCritical(new TestEvent()));
        }

        internal static void CriticalEventHandlingSuspendsRegularEventHandling() // also, critical event handling ignores this suspension, obviously
        {
            AppBaseTests.TestApp.MainEventQueue.Subscribers.Add(
                DelegateEventHandler.On<TestCriticalEvent>(
                    evnt => Assert.True(AppBaseTests.TestApp.MainEventQueue.EventHandling.IsSuspended)));

            Assert.False(AppBaseTests.TestApp.MainEventQueue.EventHandling.IsSuspended);
            AppBaseTests.TestApp.MainEventQueue.HandleCritical(new TestCriticalEvent());
            Assert.False(AppBaseTests.TestApp.MainEventQueue.EventHandling.IsSuspended);
        }
    }
}
