using Mechanical4.EventQueue;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue
{
    [TestFixture]
    public static class BlockingEnqueueExtensionTests
    {
        [Test]
        public static void EnqueueAndWaitAddsSecondEvent()
        {
            var queue = new ManualEventQueue();
            var task = queue.EnqueueAndWaitAsync(new TestEvent());
            Assert.False(task.IsCompleted);

            Assert.True(queue.HandleNext()); // test event
            Assert.False(task.IsCompleted);

            Assert.True(queue.HandleNext()); // secondary "waiting" event
            Assert.True(task.IsCompleted);

            Assert.False(queue.HandleNext()); // queue empty
            Assert.False(queue.Subscribers.Remove<EventBase>()); // subscriber of "waiting" event automatically removed
        }
    }
}
