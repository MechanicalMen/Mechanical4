using Mechanical4.EventQueue;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue
{
    [TestFixture]
    public static class FeatureSuspenderTests
    {
        [Test]
        public static void SuspendResume()
        {
            var feature = new FeatureSuspender();

            // initially enabled
            Assert.True(feature.IsEnabled);
            Assert.False(feature.IsSuspended);

            // resuming when already enabled, does nothing
            feature.Resume();
            feature.Resume();
            Assert.True(feature.IsEnabled);
            Assert.False(feature.IsSuspended);

            // initial suspension
            feature.Suspend();
            Assert.False(feature.IsEnabled);
            Assert.True(feature.IsSuspended);

            // subsequent suspension
            feature.Suspend();
            Assert.False(feature.IsEnabled);
            Assert.True(feature.IsSuspended);

            // resumption
            feature.Resume();
            Assert.False(feature.IsEnabled);
            Assert.True(feature.IsSuspended);

            feature.Resume();
            Assert.True(feature.IsEnabled);
            Assert.False(feature.IsSuspended);
        }

        [Test]
        public static void Callbacks()
        {
            int counter = 0;
            var feature = new FeatureSuspender(
                onSuspended: () => ++counter,
                onResumed: () => --counter);
            Assert.AreEqual(0, counter);

            feature.Suspend(); // suspends
            Assert.AreEqual(1, counter);

            feature.Suspend();
            Assert.AreEqual(1, counter);

            feature.Resume();
            Assert.AreEqual(1, counter);

            feature.Resume(); // resumes
            Assert.AreEqual(0, counter);
        }
    }
}
