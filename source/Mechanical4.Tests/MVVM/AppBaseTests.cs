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
            internal TestApp( EventQueueBase eventQueue )
                : base(eventQueue)
            {
            }
        }

        #endregion

        [Test]
        public static void Constructor()
        {
            var manualQueue = new ManualEventQueue();
            var app = new TestApp(manualQueue);

            Assert.NotNull(app.MainEventQueue);
            Assert.AreSame(manualQueue, app.MainEventQueue.BaseEventQueue);
            Assert.AreSame(manualQueue.Subscribers, app.MainEventQueue.Subscribers);
            Assert.AreSame(manualQueue.EventAdding, app.MainEventQueue.EventAdding);
            Assert.AreSame(manualQueue.EventHandling, app.MainEventQueue.EventHandling);
            Assert.AreSame(manualQueue.RaiseUnhandledEvents, app.MainEventQueue.RaiseUnhandledEvents);
            Assert.AreEqual(AppState.Shutdown, app.CurrentState);

            Assert.Throws<ArgumentNullException>(() => new TestApp(null));

            // try to pass an event queue that's already shut down
            manualQueue.BeginShutdown();
            while( manualQueue.HandleNext() ) ;
            Assert.Throws<ArgumentException>(() => new TestApp(manualQueue));
        }

        [Test]
        public static void ChangeToNeighboringState()
        {
            var manualQueue = new ManualEventQueue();
            var app = new TestApp(manualQueue); // starts in Shutdown state

            AppStateChangedEvent.Critical criticalEvent = null;
            app.MainEventQueue.Subscribers.Add(
                DelegateEventHandler.On<AppStateChangedEvent.Critical>(
                    e =>
                    {
                        criticalEvent = e;
                    }));

            // immediately changes state, using a critical event
            app.MoveToState(AppState.Suspended);
            Assert.AreEqual(AppState.Suspended, app.CurrentState);
            Assert.NotNull(criticalEvent);
            Assert.AreEqual(AppState.Shutdown, criticalEvent.OldState);
            Assert.AreEqual(AppState.Suspended, criticalEvent.NewState);

            // followed by a regular event
            Assert.True(manualQueue.HandleNext(out var evnt));
            var regularEvent = (AppStateChangedEvent.Regular)evnt;
            Assert.AreEqual(AppState.Suspended, regularEvent.CurrentState);
        }

        [Test]
        public static void ChangeToNonNeighboringState()
        {
            var manualQueue = new ManualEventQueue();
            var app = new TestApp(manualQueue); // starts in Shutdown state

            var criticalEvents = new List<AppStateChangedEvent.Critical>();
            app.MainEventQueue.Subscribers.Add(
                DelegateEventHandler.On<AppStateChangedEvent.Critical>(
                    e => criticalEvents.Add(e)));

            // immediately changes state
            app.MoveToState(AppState.Running);
            Assert.AreEqual(AppState.Running, app.CurrentState);

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
        }

        [Test]
        public static void ShutdownStateEnqueuesShutdownEvent()
        {
            var manualQueue = new ManualEventQueue();
            var app = new TestApp(manualQueue);
            app.MoveToState(AppState.Running);
            while( manualQueue.HandleNext() ) ;

            app.MoveToState(AppState.Shutdown);
            while( manualQueue.HandleNext() ) ;
            Assert.True(manualQueue.IsShutDown);
            Assert.AreEqual(AppState.Shutdown, app.CurrentState);
        }

        [Test]
        public static void ShutdownEventTriggersShutdownState()
        {
            var manualQueue = new ManualEventQueue();
            var app = new TestApp(manualQueue);
            app.MoveToState(AppState.Running);
            while( manualQueue.HandleNext() ) ;

            manualQueue.BeginShutdown();
            while( manualQueue.HandleNext() ) ;
            Assert.True(manualQueue.IsShutDown);
            Assert.AreEqual(AppState.Shutdown, app.CurrentState);
        }
    }
}
