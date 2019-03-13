using System;
using System.Reflection;

namespace Mechanical4.EventQueue.Primitives
{
    internal static class EventHandlerWrapper
    {
        //// NOTE: since we may want to use weak references for subscribers
        ////       we will need to store a strong reference to something else.

        #region IEventHandlerWrapper

        internal interface IEventHandlerWrapper
        {
            Type EventHandlerEventType { get; }
            TypeInfo EventHandlerEventTypeInfo { get; }

            bool? ReferenceEquals( object subscriber );
            void Handle( EventBase evnt, out bool referenceFound );
        }

        #endregion

        #region EventHandlerWrapperBase

        private class EventHandlerWrapperBase<TEvent>
            where TEvent : EventBase
        {
            private static readonly Type eventHandlerEventType = typeof(TEvent);
            private static readonly TypeInfo eventHandlerEventTypeInfo = typeof(TEvent).GetTypeInfo();

            public Type EventHandlerEventType => eventHandlerEventType;
            public TypeInfo EventHandlerEventTypeInfo => eventHandlerEventTypeInfo;
        }

        #endregion

        #region StrongEventHandlerWrapper

        private class StrongEventHandlerWrapper<TEvent> : EventHandlerWrapperBase<TEvent>, IEventHandlerWrapper
            where TEvent : EventBase
        {
            private readonly IEventHandler<TEvent> strongRef;

            internal StrongEventHandlerWrapper( IEventHandler<TEvent> subscriber )
            {
                this.strongRef = subscriber ?? throw Exc.Null(nameof(subscriber));
            }

            public bool? ReferenceEquals( object subscriber )
            {
                return ReferenceEquals(this.strongRef, subscriber);
            }

            public void Handle( EventBase evnt, out bool referenceFound )
            {
                this.strongRef.Handle((TEvent)evnt);
                referenceFound = true;
            }
        }

        #endregion

        #region WeakEventHandlerWrapper

        private class WeakEventHandlerWrapper<TEvent> : EventHandlerWrapperBase<TEvent>, IEventHandlerWrapper
            where TEvent : EventBase
        {
            private static readonly TypeInfo eventHandlerEventTypeInfo = typeof(TEvent).GetTypeInfo();
            private readonly WeakReference<IEventHandler<TEvent>> weakRef;

            internal WeakEventHandlerWrapper( IEventHandler<TEvent> subscriber )
            {
                if( subscriber.NullReference() )
                    throw Exc.Null(nameof(subscriber));

                this.weakRef = new WeakReference<IEventHandler<TEvent>>(subscriber);
            }

            public bool? ReferenceEquals( object subscriber )
            {
                if( this.weakRef.TryGetTarget(out var strongRef) )
                    return ReferenceEquals(strongRef, subscriber);
                else
                    return null;
            }

            public void Handle( EventBase evnt, out bool referenceFound )
            {
                referenceFound = this.weakRef.TryGetTarget(out var strongRef);
                if( referenceFound )
                    strongRef.Handle((TEvent)evnt);
            }
        }

        #endregion

        internal static IEventHandlerWrapper Create<TEvent>( IEventHandler<TEvent> eventHandler, bool weakRef )
            where TEvent : EventBase
        {
            if( weakRef )
                return new WeakEventHandlerWrapper<TEvent>(eventHandler);
            else
                return new StrongEventHandlerWrapper<TEvent>(eventHandler);
        }
    }
}
