using Mechanical4.EventQueue;

namespace Mechanical4.Tests.EventQueue
{
    public class TestCriticalEvent : TestEvent, ICriticalEvent
    {
        public TestCriticalEvent()
            : base()
        {
        }
    }
}
