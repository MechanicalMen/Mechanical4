using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Mechanical4.EventQueue.Primitives
{
    internal class EventSubscriberTypeInfo
    {
        #region Constructors

        private EventSubscriberTypeInfo( Type subscriberType )
        {
            this.Type = subscriberType ?? throw Exc.Null(nameof(subscriberType));
            this.TypeInfo = this.Type.GetTypeInfo();

            var types = GetEventHandlerTypes(this.Type);
            this.EventHandlerTypes = new ReadOnlyCollection<Type>(types);
            this.HandledEventTypes = new ReadOnlyCollection<Type>(GetHandledEventTypes(types));
        }

        #endregion

        #region Private methods

        private static Type[] GetEventHandlerTypes( Type subscriberType )
        {
            return subscriberType
                .GetTypeInfo()
                .ImplementedInterfaces
                .Select(interfaceType => interfaceType.GetTypeInfo())
                .Where(interfaceTypeInfo => interfaceTypeInfo.IsGenericType
                                         && interfaceTypeInfo.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                .Select(interfaceTypeInfo => interfaceTypeInfo.AsType())
                .ToArray();
        }

        private static Type[] GetHandledEventTypes( Type[] eventHandlerTypes )
        {
            return eventHandlerTypes
                .Select(interfaceType => interfaceType.GenericTypeArguments[0])
                .ToArray();
        }

        #endregion

        #region Cache

        private static readonly object syncLock = new object();
        private static readonly Dictionary<Type, EventSubscriberTypeInfo> subscriberInfos = new Dictionary<Type, EventSubscriberTypeInfo>();

        internal static EventSubscriberTypeInfo AddOrGet( Type subscriberType )
        {
            if( subscriberType.NullReference() )
                throw Exc.Null(nameof(subscriberType));

            lock( syncLock )
            {
                if( !subscriberInfos.TryGetValue(subscriberType, out var info) )
                {
                    info = new EventSubscriberTypeInfo(subscriberType);
                    subscriberInfos.Add(subscriberType, info);
                }
                return info;
            }
        }

        #endregion

        #region Dynamic method generation

        private Func<EventSubscriberCollection, object, bool, bool> addAll = null;
        private Func<EventSubscriberCollection, object, bool> removeAll = null;

        private Func<EventSubscriberCollection, object, bool> GenerateRemoveAll()
        {
            // parameters
            var subscriberCollectionParameter = Expression.Parameter(typeof(EventSubscriberCollection));
            var subscriberParameter = Expression.Parameter(typeof(object));

            // local variables
            var atLeastOneEventHandlerRemoved = Expression.Parameter(typeof(bool));

            // subscriberCollection.Remove<TEvent>( (IEventHandler<TEvent>)subscriber )
            var genericMethod = typeof(EventSubscriberCollection).GetTypeInfo().GetDeclaredMethod(nameof(EventSubscriberCollection.Remove));
            Expression CreateMethodCall( Type eventHandlerType, Type eventHandlerEventType )
            {
                var fromObjectToEventHandler = Expression.Convert(
                    subscriberParameter,
                    eventHandlerType);

                return Expression.Call(
                    instance: subscriberCollectionParameter,
                    method: genericMethod.MakeGenericMethod(eventHandlerEventType),
                    arguments: new Expression[] { fromObjectToEventHandler });
            }

            // if( remove() ) atLeastOneEventHandlerRemoved = true
            Expression CreateIf( Type eventHandlerType, Type eventHandlerEventType )
            {
                return Expression.IfThen(
                    test: CreateMethodCall(eventHandlerType, eventHandlerEventType),
                    ifTrue: Expression.Assign(
                        atLeastOneEventHandlerRemoved,
                        Expression.Constant(true)));
            }

            // foreach( eventHandler ) <create> <if-expression>
            var ifExpressions = new Expression[this.EventHandlerTypes.Count];
            for( int i = 0; i < ifExpressions.Length; ++i )
                ifExpressions[i] = CreateIf(this.EventHandlerTypes[i], this.HandledEventTypes[i]);

            // create body of RemoveAll
            var body = new Expression[]
            {
                // atLeastOneEventHandlerRemoved = false
                Expression.Assign(
                    atLeastOneEventHandlerRemoved,
                    Expression.Constant(false))
            }
            .Concat(ifExpressions)
            .Concat(new[] { atLeastOneEventHandlerRemoved }); // last expression in a Block becomes the return value

            // create lambda expression
            var lambda = Expression.Lambda<Func<EventSubscriberCollection, object, bool>>(
                body: Expression.Block(
                    variables: new ParameterExpression[] { atLeastOneEventHandlerRemoved },
                    expressions: body),
                parameters: new ParameterExpression[] { subscriberCollectionParameter, subscriberParameter });

            return lambda.Compile();
        }

        private Func<EventSubscriberCollection, object, bool, bool> GenerateAddAll()
        {
            // parameters
            var subscriberCollectionParameter = Expression.Parameter(typeof(EventSubscriberCollection));
            var subscriberParameter = Expression.Parameter(typeof(object));
            var weakRefParameter = Expression.Parameter(typeof(bool));

            // local variables
            var atLeastOneEventHandlerAdded = Expression.Parameter(typeof(bool));

            // subscriberCollection.Remove<TEvent>( (IEventHandler<TEvent>)subscriber )
            var genericMethod = typeof(EventSubscriberCollection).GetTypeInfo().GetDeclaredMethod(nameof(EventSubscriberCollection.Add));
            Expression CreateMethodCall( Type eventHandlerType, Type eventHandlerEventType )
            {
                var fromObjectToEventHandler = Expression.Convert(
                    subscriberParameter,
                    eventHandlerType);

                return Expression.Call(
                    instance: subscriberCollectionParameter,
                    method: genericMethod.MakeGenericMethod(eventHandlerEventType),
                    arguments: new Expression[] { fromObjectToEventHandler, weakRefParameter });
            }

            // if( add() ) atLeastOneEventHandlerAdded = true
            Expression CreateIf( Type eventHandlerType, Type eventHandlerEventType )
            {
                return Expression.IfThen(
                    test: CreateMethodCall(eventHandlerType, eventHandlerEventType),
                    ifTrue: Expression.Assign(
                        atLeastOneEventHandlerAdded,
                        Expression.Constant(true)));
            }

            // foreach( eventHandler ) <create> <if-expression>
            var ifExpressions = new Expression[this.EventHandlerTypes.Count];
            for( int i = 0; i < ifExpressions.Length; ++i )
                ifExpressions[i] = CreateIf(this.EventHandlerTypes[i], this.HandledEventTypes[i]);

            // create body of RemoveAll
            var body = new Expression[]
            {
                // atLeastOneEventHandlerAdded = false
                Expression.Assign(
                    atLeastOneEventHandlerAdded,
                    Expression.Constant(false))
            }
            .Concat(ifExpressions)
            .Concat(new[] { atLeastOneEventHandlerAdded }); // last expression in a Block becomes the return value

            // create lambda expression
            var lambda = Expression.Lambda<Func<EventSubscriberCollection, object, bool, bool>>(
                body: Expression.Block(
                    variables: new ParameterExpression[] { atLeastOneEventHandlerAdded },
                    expressions: body),
                parameters: new ParameterExpression[] { subscriberCollectionParameter, subscriberParameter, weakRefParameter });

            return lambda.Compile();
        }

        #endregion

        #region Internal Members

        internal Type Type { get; }
        internal TypeInfo TypeInfo { get; }

        internal ReadOnlyCollection<Type> EventHandlerTypes { get; }
        internal ReadOnlyCollection<Type> HandledEventTypes { get; }

        internal bool AddAll( EventSubscriberCollection subscriberCollection, object subscriber, bool weakRef )
        {
            var func = Interlocked.CompareExchange(ref this.addAll, null, null);
            if( func.NullReference() )
            {
                func = GenerateAddAll();
                Interlocked.CompareExchange(ref this.addAll, func, null);
            }
            return func(subscriberCollection, subscriber, weakRef);
        }

        internal bool RemoveAll( EventSubscriberCollection subscriberCollection, object subscriber )
        {
            var func = Interlocked.CompareExchange(ref this.removeAll, null, null);
            if( func.NullReference() )
            {
                func = GenerateRemoveAll();
                Interlocked.CompareExchange(ref this.removeAll, func, null);
            }
            return func(subscriberCollection, subscriber);
        }

        #endregion
    }
}
