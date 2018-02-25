using Mechanical4.EventQueue;
using Mechanical4.EventQueue.Events;

namespace Mechanical4.Tests.EventQueue
{
    public class TestEventHandler : IEventHandler<NamedEvent>, IEventHandler<TestEvent>
    {
        public void Handle( NamedEvent evnt )
        {
            this.LastEventHandled = evnt;
        }

        public virtual void Handle( TestEvent evnt )
        {
            this.LastEventHandled = evnt;
        }

        public EventBase LastEventHandled { get; private set; }
    }
}
