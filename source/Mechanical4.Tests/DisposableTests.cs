using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Mechanical4.Tests
{
    [TestFixture]
    public static class DisposableTests
    {
        #region Test types

        private class DummyInvokeTest
        {
            public int Value { get; private set; }
            public void Increase() => ++this.Value;
        }

        private interface IDisposableDummy : Disposable.IDisposableObject
        {
            DummyInvokeTest Dummy { get; }

            void ThrowIfDisposed();
        }

        private class NonBlockingDummy : Disposable.NonBlockingBase, IDisposableDummy
        {
            public DummyInvokeTest Dummy { get; } = new DummyInvokeTest();

            protected override void OnDispose()
            {
                this.Dummy.Increase();
            }

            public void ThrowIfDisposed()
            {
                base.ThrowIfDisposed();
            }
        }

        private class BlockingDummy : Disposable.BlockingBase, IDisposableDummy
        {
            public DummyInvokeTest Dummy { get; } = new DummyInvokeTest();

            protected override void OnDispose()
            {
                this.Dummy.Increase();
            }

            public void ThrowIfDisposed()
            {
                base.ThrowIfDisposed();
            }
        }

        private class NonBlockingLongDispose : Disposable.NonBlockingBase
        {
            protected override void OnDispose()
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(300));
            }
        }

        private class BlockingLongDispose : Disposable.BlockingBase
        {
            protected override void OnDispose()
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(300));
            }
        }

        #endregion

        [Test]
        public static void InvokeAtMostOnce_NullDelegate()
        {
            var invoker = new Disposable.InvokeAtMostOnce(null);
            Assert.False(invoker.NextInvokeExecutes);
            Assert.False(invoker.Invoke());
            Assert.False(invoker.NextInvokeExecutes);
            Assert.False(invoker.Invoke());
        }

        [Test]
        public static void InvokeAtMostOnce_ExactlyOneInvoke()
        {
            var dummy = new DummyInvokeTest();
            var invoker = new Disposable.InvokeAtMostOnce(dummy.Increase);

            // initially not invoked
            Assert.True(invoker.NextInvokeExecutes);
            Assert.AreEqual(0, dummy.Value);

            // first invoke executes delegate
            Assert.True(invoker.Invoke());
            Assert.AreEqual(1, dummy.Value);
            Assert.False(invoker.NextInvokeExecutes);

            // subsequent invokes have no effect
            foreach( var i in Enumerable.Range(0, 3) )
            {
                Assert.False(invoker.Invoke());
                Assert.AreEqual(1, dummy.Value);
                Assert.False(invoker.NextInvokeExecutes);
            }
        }

        private static void DisposableBase_ExactlyOneDispose( IDisposableDummy testObj )
        {
            Assert.NotNull(testObj);

            // initially not disposed
            Assert.False(testObj.IsDisposed);
            Assert.AreEqual(0, testObj.Dummy.Value);

            // first Dispose executes
            testObj.Dispose();
            Assert.True(testObj.IsDisposed);
            Assert.AreEqual(1, testObj.Dummy.Value);

            // subsequent calls have no effect
            foreach( var i in Enumerable.Range(0, 3) )
            {
                testObj.Dispose();
                Assert.True(testObj.IsDisposed);
                Assert.AreEqual(1, testObj.Dummy.Value);
            }
        }

        private static void DisposableBase_ThrowIfDisposed( IDisposableDummy testObj )
        {
            Assert.NotNull(testObj);
            Assert.False(testObj.IsDisposed);

            testObj.ThrowIfDisposed();
            testObj.Dispose();
            Assert.Throws<ObjectDisposedException>(() => testObj.ThrowIfDisposed());
        }

        private static void DisposableBase_ConcurrentDispose( Disposable.IDisposableObject testObj, bool callsExpectedToFinishAboutTheSameTime )
        {
            Assert.NotNull(testObj);
            Assert.False(testObj.IsDisposed);

            // start multiple Dispose calls: first one, than others concurrently
            var stopwatch = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, 5)
                .Select(index =>
                {
                    var t = Task.Run(() =>
                    {
                        testObj.Dispose();
                        return (Index: index, FinishedTimestamp: stopwatch.Elapsed);
                    });
                    if( index == 0 )
                        Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    return t;
                })
                .ToArray();

            // wait for all Dispose calls to finish
            Task.WaitAll(tasks);

            // check results
            var firstDisposeTimestamp = tasks[0].Result.FinishedTimestamp;
            bool tasksFinishedCloseToEachOther = tasks.Select(t => Math.Abs((t.Result.FinishedTimestamp - firstDisposeTimestamp).Ticks)).Max() < TimeSpan.FromMilliseconds(50).Ticks;
            Assert.AreEqual(callsExpectedToFinishAboutTheSameTime, tasksFinishedCloseToEachOther);
        }

        [Test]
        public static void NonBlockingBaseTests()
        {
            DisposableBase_ExactlyOneDispose(new NonBlockingDummy());
            DisposableBase_ThrowIfDisposed(new NonBlockingDummy());
            DisposableBase_ConcurrentDispose(new NonBlockingLongDispose(), callsExpectedToFinishAboutTheSameTime: false);
        }

        [Test]
        public static void BlockingBaseTests()
        {
            DisposableBase_ExactlyOneDispose(new BlockingDummy());
            DisposableBase_ThrowIfDisposed(new BlockingDummy());
            DisposableBase_ConcurrentDispose(new BlockingLongDispose(), callsExpectedToFinishAboutTheSameTime: true);
        }
    }
}
