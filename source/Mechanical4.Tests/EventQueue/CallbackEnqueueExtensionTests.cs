using Mechanical4.EventQueue;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue
{
    [TestFixture]
    public static class CallbackEnqueueExtensionTests
    {
        [Test]
        public static void Callback()
        {
            var queue = new ManualEventQueue();
            var evnt = new TestEvent();
            bool handled = false;

            queue.Enqueue(
                evnt,
                onHandled: e =>
                {
                    Assert.True(ReferenceEquals(e, evnt));
                    handled = true;
                });

            // handled original event
            Assert.False(handled);
            Assert.True(queue.HandleNext());
            Assert.False(handled);

            // handle second event, implicitly added
            Assert.True(queue.HandleNext());
            Assert.True(handled);

            // no other events were added
            Assert.False(queue.HandleNext());
        }

        [Test]
        public static void NullCallback()
        {
            var queue = new ManualEventQueue();

            Assert.True(queue.Enqueue(new TestEvent(), onHandled: null)); // no exception, successful call
            Assert.True(queue.HandleNext());
            Assert.False(queue.HandleNext()); // no secondary event added
        }
    }
}
