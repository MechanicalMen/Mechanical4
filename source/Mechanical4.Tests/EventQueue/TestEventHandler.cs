using Mechanical4.EventQueue.Events;

namespace Mechanical4.EventQueue.Tests
{
    public class TestEventHandler : IEventHandler<NamedEvent>, IEventHandler<TestEvent>
    {
        public void Handle( NamedEvent evnt )
        {
            this.LastEventHandled = evnt;
        }

        public void Handle( TestEvent evnt )
        {
            this.LastEventHandled = evnt;
        }

        public EventBase LastEventHandled { get; private set; }
    }
}
