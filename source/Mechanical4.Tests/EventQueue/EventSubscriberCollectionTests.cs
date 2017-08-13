using System;
using Mechanical4.EventQueue.Events;
using NUnit.Framework;

namespace Mechanical4.EventQueue.Tests
{
    [TestFixture]
    public static class EventSubscriberCollectionTests
    {
        [Test]
        public static void AddRemoveSubscriberInstance()
        {
            var subscriber = new TestEventHandler();
            var collection = new EventSubscriberCollection();

            // invalid argument tests
            Assert.Throws<ArgumentNullException>(() => collection.Add(null));
            Assert.Throws<ArgumentNullException>(() => collection.Remove(null));

            // can not remove, what's not there
            Assert.False(collection.Remove(subscriber));

            // add
            Assert.True(collection.Add(subscriber, useWeakRef: false));
            Assert.False(collection.Add(subscriber, useWeakRef: false)); // subscribers only added once
            Assert.False(collection.Add(subscriber, useWeakRef: true)); // the type of reference used does not change this

            // remove
            Assert.True(collection.Remove(subscriber));
            Assert.False(collection.Remove(subscriber));
        }

        [Test]
        public static void RemoveSubscribersOfEvent()
        {
            var subscriber = new TestEventHandler();
            var collection = new EventSubscriberCollection();
            collection.Add(subscriber, useWeakRef: false);

            // remove one of the event handlers...
            Assert.True(collection.Remove<NamedEvent>());
            Assert.False(collection.Remove<NamedEvent>());

            // ... the other remains
            Assert.True(collection.Remove(subscriber));

            // or remove them both
            collection.Add(subscriber);
            Assert.True(collection.Remove<EventBase>());
            Assert.False(collection.Remove(subscriber));
        }

        [Test]
        public static void Clear()
        {
            var subscriber = new TestEventHandler();
            var collection = new EventSubscriberCollection();

            collection.Add(subscriber, useWeakRef: false);
            collection.Clear();
            Assert.False(collection.Remove(subscriber));
        }

        [Test]
        public static void WeakSubscriberReference()
        {
            var collection = new EventSubscriberCollection();
            WeakReference<TestEventHandler> weakRef;

            {
                var subscriber = new TestEventHandler();
                weakRef = new WeakReference<TestEventHandler>(subscriber);
                collection.Add(subscriber, useWeakRef: true);
                subscriber = null;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.False(weakRef.TryGetTarget(out _));
            GC.KeepAlive(collection);
        }
    }
}
