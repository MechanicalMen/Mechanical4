using System;
using System.Collections.Generic;
using Mechanical4.EventQueue;
using Mechanical4.MVVM;
using NUnit.Framework;

namespace Mechanical4.Tests.MVVM
{
    [TestFixture]
    public static class AppBaseTests
    {
        #region TestApp

        internal class TestApp : AppBase
        {
            private TestApp()
            {
            }

            internal static new void Initialize( EventQueueBase eventQueue )
            {
                AppBase.Initialize(eventQueue);
            }
        }

        #endregion

        [Test]
        public static void CombinedTests()
        {
            Constructor(out var manualQueue);
            ChangeToNonNeighboringState(manualQueue);
            ChangeToNeighboringState(manualQueue);
            ChangeToSameState(manualQueue);

            MainEventQueueTests.RegularEventHandlingWorksAsExpected();
            MainEventQueueTests.CriticalEventsAreHandledImmediately();
            MainEventQueueTests.RegularAndCriticalEventsMustUseCorrespondingMethod();
            MainEventQueueTests.CriticalEventHandlingSuspendsRegularEventHandling();

            if( (new Random().Next() % 2) == 0 )
                ShutdownStateEnqueuesShutdownEvent(manualQueue);
            else
                ShutdownEventTriggersShutdownState(manualQueue);
        }

        private static void Constructor( out ManualEventQueue queue )
        {
            // try to pass a null event queue
            Assert.Throws<ArgumentNullException>(() => TestApp.Initialize(null));

            // try to pass an event queue that's already shut down
            var manualQueue = new ManualEventQueue();
            manualQueue.BeginShutdown();
            while( manualQueue.HandleNext() ) ;
            Assert.Throws<ArgumentException>(() => TestApp.Initialize(manualQueue));

            // pass a proper event queue
            manualQueue = new ManualEventQueue();
            TestApp.Initialize(manualQueue);

            Assert.NotNull(AppBase.MainEventQueue);
            Assert.AreSame(manualQueue, AppBase.MainEventQueue.BaseEventQueue);
            Assert.AreSame(manualQueue.Subscribers, AppBase.MainEventQueue.Subscribers);
            Assert.AreSame(manualQueue.EventAdding, AppBase.MainEventQueue.EventAdding);
            Assert.AreSame(manualQueue.EventHandling, AppBase.MainEventQueue.EventHandling);
            Assert.AreSame(manualQueue.RaiseUnhandledEvents, AppBase.MainEventQueue.RaiseUnhandledEvents);
            Assert.AreEqual(AppState.Shutdown, AppBase.CurrentState);

            queue = manualQueue;
        }

        private static void ChangeToNeighboringState( ManualEventQueue manualQueue ) // Running --> Suspended
        {
            AppStateChangedEvent.Critical criticalEvent = null;
            var subscriber = DelegateEventHandler.On<AppStateChangedEvent.Critical>(
                e =>
                {
                    criticalEvent = e;
                });
            TestApp.MainEventQueue.Subscribers.Add(subscriber);

            // immediately changes state, using a critical event
            TestApp.MoveToState(AppState.Suspended);
            Assert.AreEqual(AppState.Suspended, TestApp.CurrentState);
            Assert.NotNull(criticalEvent);
            Assert.AreEqual(AppState.Running, criticalEvent.OldState);
            Assert.AreEqual(AppState.Suspended, criticalEvent.NewState);

            // followed by a regular event
            Assert.True(manualQueue.HandleNext(out var evnt));
            var regularEvent = (AppStateChangedEvent.Regular)evnt;
            Assert.AreEqual(AppState.Suspended, regularEvent.CurrentState);

            TestApp.MainEventQueue.Subscribers.Remove(subscriber);
        }

        private static void ChangeToNonNeighboringState( ManualEventQueue manualQueue ) // Shutdown --> Running
        {
            var criticalEvents = new List<AppStateChangedEvent.Critical>();
            var subscriber = DelegateEventHandler.On<AppStateChangedEvent.Critical>(
                e => criticalEvents.Add(e));
            TestApp.MainEventQueue.Subscribers.Add(subscriber);

            // immediately changes state
            TestApp.MoveToState(AppState.Running);
            Assert.AreEqual(AppState.Running, TestApp.CurrentState);

            // critical events already handled
            Assert.AreEqual(2, criticalEvents.Count);

            var criticalEvent = criticalEvents[0];
            Assert.AreEqual(AppState.Shutdown, criticalEvent.OldState);
            Assert.AreEqual(AppState.Suspended, criticalEvent.NewState);

            criticalEvent = criticalEvents[1];
            Assert.AreEqual(AppState.Suspended, criticalEvent.OldState);
            Assert.AreEqual(AppState.Running, criticalEvent.NewState);

            // followed by a regular event
            Assert.True(manualQueue.HandleNext(out var evnt));
            var regularEvent = (AppStateChangedEvent.Regular)evnt;
            Assert.AreEqual(AppState.Running, regularEvent.CurrentState);

            TestApp.MainEventQueue.Subscribers.Remove(subscriber);
        }

        private static void ChangeToSameState( ManualEventQueue manualQueue )
        {
            AppStateChangedEvent.Critical criticalEvent = null;
            var subscriber = DelegateEventHandler.On<AppStateChangedEvent.Critical>(
                e =>
                {
                    criticalEvent = e;
                });
            TestApp.MainEventQueue.Subscribers.Add(subscriber);

            var currentState = TestApp.CurrentState;
            TestApp.MoveToState(currentState);
            Assert.AreEqual(currentState, TestApp.CurrentState); // no state change
            Assert.Null(criticalEvent); // no critical event
            Assert.False(manualQueue.HandleNext()); // no regular event

            TestApp.MainEventQueue.Subscribers.Remove(subscriber);
        }

        private static void ShutdownStateEnqueuesShutdownEvent( ManualEventQueue manualQueue )
        {
            TestApp.MoveToState(AppState.Running);
            while( manualQueue.HandleNext() ) ;

            TestApp.MoveToState(AppState.Shutdown);
            while( manualQueue.HandleNext() ) ;
            Assert.True(manualQueue.IsShutDown);
            Assert.AreEqual(AppState.Shutdown, TestApp.CurrentState);
        }

        private static void ShutdownEventTriggersShutdownState( ManualEventQueue manualQueue )
        {
            TestApp.MoveToState(AppState.Running);
            while( manualQueue.HandleNext() ) ;

            manualQueue.BeginShutdown();
            while( manualQueue.HandleNext() ) ;
            Assert.True(manualQueue.IsShutDown);
            Assert.AreEqual(AppState.Shutdown, TestApp.CurrentState);
        }
    }
}
