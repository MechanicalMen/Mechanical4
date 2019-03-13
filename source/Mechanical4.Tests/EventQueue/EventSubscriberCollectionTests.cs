using System;
using Mechanical4.EventQueue;
using Mechanical4.EventQueue.Primitives;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue
{
    [TestFixture]
    public static class EventSubscriberCollectionTests
    {
        [Test]
        public static void AddRemoveIndividualEventHandlers()
        {
            var subscriber = new TestEventHandler();
            var collection = new EventSubscriberCollection();

            // invalid argument tests
            Assert.Throws<ArgumentNullException>(() => collection.Add<EventBase>(null, weakRef: true));
            Assert.Throws<ArgumentNullException>(() => collection.Add<EventBase>(null, weakRef: false));
            Assert.Throws<ArgumentNullException>(() => collection.Remove<EventBase>(null));

            // can not remove, what's not there
            Assert.False(collection.Remove<NamedEvent>(subscriber));
            Assert.False(collection.Remove<TestEvent>(subscriber));

            // add
            Assert.True(collection.Add<NamedEvent>(subscriber, weakRef: false));
            Assert.False(collection.Add<NamedEvent>(subscriber, weakRef: false)); // subscribers only added once
            Assert.False(collection.Add<NamedEvent>(subscriber, weakRef: true)); // the type of reference used does not change this

            // remove
            Assert.True(collection.Remove<NamedEvent>(subscriber));
            Assert.False(collection.Remove<NamedEvent>(subscriber));
        }

        [Test]
        public static void AddRemoveMultipleIndividualHandlersFromSameSubscriber()
        {
            var subscriber = new TestEventHandler();
            var collection = new EventSubscriberCollection();

            Assert.True(collection.Add<NamedEvent>(subscriber));
            Assert.False(collection.Add<NamedEvent>(subscriber));

            Assert.True(collection.Add<TestEvent>(subscriber));
            Assert.False(collection.Add<TestEvent>(subscriber));

            Assert.True(collection.Remove<NamedEvent>(subscriber));
            Assert.False(collection.Remove<NamedEvent>(subscriber));

            Assert.True(collection.Remove<TestEvent>(subscriber));
            Assert.False(collection.Remove<TestEvent>(subscriber));
        }

        [Test]
        public static void AddRemoveAllEventHandlers()
        {
            var subscriber = new TestEventHandler();
            var collection = new EventSubscriberCollection();

            // invalid argument tests
            Assert.Throws<ArgumentNullException>(() => collection.AddAll(null, weakRef: true));
            Assert.Throws<ArgumentNullException>(() => collection.AddAll(null, weakRef: false));
            Assert.Throws<ArgumentNullException>(() => collection.RemoveAll(null));

            // can not remove, what's not there
            Assert.False(collection.RemoveAll(subscriber));

            // add
            Assert.True(collection.AddAll(subscriber, weakRef: false));
            Assert.False(collection.AddAll(subscriber, weakRef: false)); // subscribers only added once
            Assert.False(collection.AddAll(subscriber, weakRef: true)); // the type of reference used does not change this

            // remove
            Assert.True(collection.RemoveAll(subscriber));
            Assert.False(collection.RemoveAll(subscriber));
        }

        [Test]
        public static void MixedIndividualOrCompleteAdditionRemoval()
        {
            var subscriber = new TestEventHandler();
            var collection = new EventSubscriberCollection();

            // add individually
            Assert.True(collection.Add<NamedEvent>(subscriber));
            Assert.True(collection.Add<TestEvent>(subscriber));

            // remove all
            Assert.True(collection.RemoveAll(subscriber));

            // check individually
            Assert.False(collection.Remove<NamedEvent>(subscriber));
            Assert.False(collection.Remove<TestEvent>(subscriber));


            // add all
            Assert.True(collection.AddAll(subscriber));

            // remove individually
            Assert.True(collection.Remove<NamedEvent>(subscriber));
            Assert.True(collection.Remove<TestEvent>(subscriber));

            // check all
            Assert.False(collection.RemoveAll(subscriber));


            // add all
            Assert.True(collection.AddAll(subscriber));

            // remove some
            Assert.True(collection.Remove<NamedEvent>(subscriber));

            // remove rest
            Assert.True(collection.RemoveAll(subscriber));

            // check
            Assert.False(collection.RemoveAll(subscriber));
        }

        [Test]
        public static void Clear()
        {
            var subscriber = new TestEventHandler();
            var collection = new EventSubscriberCollection();

            collection.AddAll(subscriber);
            collection.Clear();
            Assert.False(collection.RemoveAll(subscriber));
        }

        [Test]
        public static void WeakSubscriberReference()
        {
            var collection = new EventSubscriberCollection();
            WeakReference<TestEventHandler> weakRef;

            // register subscriber using weak references
            // then release our strong reference
            {
                var subscriber = new TestEventHandler();
                weakRef = new WeakReference<TestEventHandler>(subscriber);
                collection.AddAll(subscriber, weakRef: true);
                subscriber = null;
            }

            // garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // we have proof that the collection did not use strong references
            Assert.False(weakRef.TryGetTarget(out _));
            GC.KeepAlive(collection);
        }

        [Test]
        public static void StrongSubscriberReference()
        {
            var collection = new EventSubscriberCollection();
            WeakReference<TestEventHandler> weakRef;

            // register subscriber using strong references
            // then release our strong reference
            {
                var subscriber = new TestEventHandler();
                weakRef = new WeakReference<TestEventHandler>(subscriber);
                collection.AddAll(subscriber, weakRef: false);
                subscriber = null;
            }

            // garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // we have proof that the collection did use strong references
            Assert.True(weakRef.TryGetTarget(out _));
            GC.KeepAlive(collection);
        }
    }
}
