using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// Represents a chain of properties (e.g. obj.P1.P2.P3),
    /// and invokes a delegate when the value of the last property changes.
    /// Handles <see cref="INotifyPropertyChanged.PropertyChanged"/>, if available.
    /// Other than the chain builders, this class is thread-safe.
    /// </summary>
    public abstract class PropertyChangedChain : Disposable.NonBlockingBase
    {
        //// NOTE: the main requirements are:
        ////        - getting property values should be as fast as possible (no reflection other than at creation) (possibly allow reflection for properties that change very rarely, delaying the cost of initial compilation)
        ////        - optionally only report a change, when the property value actually changed
        ////          (as opposed to whenever an event fires somewhere in the chain)
        ////        - allow non-INotifyPropertyChanged objects in the chain
        ////        - allow value types in the chain
        ////        - be thread-safe
        ////        - it is OK to keep strong references to the last recorded property values, as well as the source object

        //// NOTE: we could trivially allow changing the starting object of the chain, but there is currently no need for that.

        #region CanBeAssignedNull, IsReferenceType, ImplementsInterface

        //// NOTE: based on: https://stackoverflow.com/a/374663

        private static bool CanBeAssignedNull<T>()
        {
            Type type = typeof(T);
#if NETSTANDARD1_1
            var typeInfo = type.GetTypeInfo();
            if( !typeInfo.IsValueType ) return true; // ref-type
            if( Nullable.GetUnderlyingType(type) != null ) return true; // Nullable<T>
            return false; // value-type
#else
            if( !type.IsValueType ) return true; // ref-type
            if( Nullable.GetUnderlyingType(type) != null ) return true; // Nullable<T>
            return false; // value-type
#endif
        }

        private static bool IsReferenceType<T>() // see: CanBeAssignedNull
        {
#if NETSTANDARD1_1
            return !typeof(T).GetTypeInfo().IsValueType;
#else
            return !typeof(T).IsValueType;
#endif
        }

        private static bool ImplementsInterface<T, I>()
        {
#if NETSTANDARD1_1
            Type[] interfaces = typeof(T).GetTypeInfo().ImplementedInterfaces.ToArray();
#else
            Type[] interfaces = typeof(T).GetInterfaces();
#endif
            foreach( var t in interfaces )
            {
                if( t == typeof(I) )
                    return true;
            }
            return false;
        }

        #endregion

        #region IChainLink, IChainLinkConnector

        private interface IChainLink<TSource>
        {
            void Initialize( TSource source );

            void OnParentPropertyValueChanged( TSource oldValue, TSource newValue, bool forceChange );
        }

        private interface IChainLinkConnector<TProperty>
        {
            void SetNextLink( IChainLink<TProperty> nextLink );
        }

        #endregion

        #region ChainChangeNotifier

        private class ChainChangeNotifier<T> : IChainLink<T>
        {
            private readonly Action<T, T> notify;

            internal ChainChangeNotifier( Action<T, T> action )
            {
                this.notify = action ?? throw Exc.Null(nameof(action));
            }

            public void Initialize( T _ )
            {
            }

            public void OnParentPropertyValueChanged( T oldValue, T newValue, bool forceChange )
            {
                this.notify(oldValue, newValue);
            }
        }

        #endregion

        #region DirectPropertyChangedListener

        private class DirectPropertyChangedListener<TSource, TProperty>
            : Disposable.NonBlockingBase,
              IChainLink<TSource>,
              IChainLinkConnector<TProperty>
        {
            //// possible micro-optimization: share the locking object between chain links

            #region Private Fields

            private readonly object syncLock = new object();
            private readonly string propertyName;
            private readonly Func<TSource, TProperty> getPropertyValue;
            private readonly IEqualityComparer<TProperty> comparer;
            private TSource source;
            private TProperty lastValue;
            private IChainLink<TProperty> nextChainLink;

            #endregion

            #region Constructor

            internal DirectPropertyChangedListener(
                string property,
                Func<TSource, TProperty> getProperty,
                IEqualityComparer<TProperty> propertyComparer )
            {
                if( property.NullReference() )
                    throw Exc.Null("Invalid property name: null!");

                if( string.IsNullOrWhiteSpace(property)
                 || char.IsWhiteSpace(property[0])
                 || char.IsWhiteSpace(property[property.Length - 1]) )
                    throw new ArgumentException($"Invalid property name: \"{property}\"!");

                this.propertyName = property;
                this.getPropertyValue = getProperty ?? throw Exc.Null(nameof(getProperty));
                this.comparer = propertyComparer; // may be null!
                this.source = default;
                this.lastValue = default;
            }

            #endregion

            #region Private Methods

            private TProperty GetPropertyValue()
            {
                if( FullChain<TSource>.SourceMayBeNull
                 && this.source.NullReference() )
                    return default;
                else
                    return this.getPropertyValue(this.source);
            }

            private void OnPropertyChanged( object sender, PropertyChangedEventArgs e )
            {
                this.ThrowIfDisposed(); // this should not happen, but it can't hurt to make sure

                if( string.Equals(this.propertyName, e.PropertyName, StringComparison.Ordinal) )
                    this.CheckPropertyForChange(forceChange: false);
            }

            private void CheckPropertyForChange( bool forceChange )
            {
                lock( this.syncLock )
                {
                    var oldValue = this.lastValue;
                    var newValue = this.GetPropertyValue();
                    if( forceChange
                     || !(this.comparer?.Equals(oldValue, newValue) ?? false) ) // no comparer, or the value changed
                    {
                        this.lastValue = newValue;
                        this.nextChainLink.OnParentPropertyValueChanged(oldValue, newValue, forceChange);
                    }
                }
            }

            #endregion

            #region IChainLink, IChainLinkConnector

            public void Initialize( TSource source )
            {
                this.source = source;
                if( this.source is INotifyPropertyChanged notify )
                    notify.PropertyChanged += this.OnPropertyChanged;

                this.lastValue = this.GetPropertyValue();
                this.nextChainLink?.Initialize(this.lastValue);
            }

            public void OnParentPropertyValueChanged( TSource oldValue, TSource newValue, bool forceChange )
            {
                this.ThrowIfDisposed(); // this should not happen, but it can't hurt to make sure

                lock( this.syncLock )
                {
                    if( FullChain<TSource>.IsRefType
                     && ReferenceEquals(oldValue, newValue) )
                    {
                        //// it's the same object
                    }
                    else
                    {
                        //// either value type, or different references

                        if( !FullChain<TSource>.ImplementsINotifyPropertyChanged )
                        {
                            this.source = newValue;
                        }
                        else
                        {
                            var oldNotify = oldValue as INotifyPropertyChanged;
                            if( oldNotify.NotNullReference() ) // just because we know the interface is implemented, does not keep the object from being null
                                oldNotify.PropertyChanged -= this.OnPropertyChanged; // do not change the source, while we have an event handler for it

                            this.source = newValue;

                            var newNotify = newValue as INotifyPropertyChanged;
                            if( newNotify.NotNullReference() )
                                newNotify.PropertyChanged += this.OnPropertyChanged; // do not add an event handler, until we changed our source
                        }
                    }

                    // check for change
                    this.CheckPropertyForChange(forceChange);
                }
            }

            public void SetNextLink( IChainLink<TProperty> nextLink )
            {
                lock( this.syncLock )
                    this.nextChainLink = nextLink ?? throw Exc.Null(nameof(nextLink));
            }

            #endregion

            #region IDisposable

            /// <summary>
            /// Releases managed (IDisposable) resources.
            /// </summary>
            protected override void OnDispose()
            {
                lock( this.syncLock )
                {
                    if( this.source is INotifyPropertyChanged notify )
                        notify.PropertyChanged -= this.OnPropertyChanged;

                    this.source = default;
                    this.lastValue = default;
                    this.nextChainLink = null;
                }
            }

            #endregion
        }

        #endregion

        #region Builder

        /// <summary>
        /// Helps creating a <see cref="PropertyChangedChain"/> instance.
        /// </summary>
        /// <typeparam name="TStart">The type of the first object in the chain, from which to get the first property of.</typeparam>
        /// <typeparam name="TSource">The type of the current object in the chain, as well as the type of the previous property in the chain.</typeparam>
        public class Builder<TStart, TSource>
        {
            #region Private Fields

            private readonly List<object> chain;
            private readonly TStart start;

            #endregion

            #region Constructor

            internal Builder( List<object> list, TStart startObj )
            {
                this.chain = list ?? throw Exc.Null(nameof(list));
                this.start = startObj;
            }

            #endregion

            #region Property

            /// <summary>
            /// Specifies the next property in the chain.
            /// </summary>
            /// <typeparam name="TProperty">The type of the property.</typeparam>
            /// <param name="propertyName">The name of the property.</param>
            /// <param name="getPropertyValue">A delegate to get the value of the property named <paramref name="propertyName"/>.</param>
            /// <param name="comparer">An optional <see cref="IEqualityComparer{T}"/> used to filter out duplicate property change notifications.</param>
            /// <returns>An object that helps creating a <see cref="PropertyChangedChain"/>, using it's fluent interface.</returns>
            public Builder<TStart, TProperty> Property<TProperty>(
                string propertyName,
                Func<TSource, TProperty> getPropertyValue,
                IEqualityComparer<TProperty> comparer = null )
            {
                var newLink = new DirectPropertyChangedListener<TSource, TProperty>(propertyName, getPropertyValue, comparer);

                if( this.chain.Count != 0 )
                {
                    var lastLink = (IChainLinkConnector<TSource>)this.chain.Last();
                    lastLink.SetNextLink(newLink);
                }
                this.chain.Add(newLink);

                return new Builder<TStart, TProperty>(this.chain, this.start);
            }

            /// <summary>
            /// Specifies the next property in the chain.
            /// This is slower than the other overload, but that shouldn't be noticeable in most cases (and when in doubt: profile first!).
            /// </summary>
            /// <typeparam name="TProperty">The type of the property.</typeparam>
            /// <param name="getProperty">The expression to get the name and getter of the property from.</param>
            /// <param name="comparer">An optional <see cref="IEqualityComparer{T}"/> used to filter out duplicate property change notifications.</param>
            /// <returns>An object that helps creating a <see cref="PropertyChangedChain"/>, using it's fluent interface.</returns>
            public Builder<TStart, TProperty> Property<TProperty>(
                Expression<Func<TSource, TProperty>> getProperty,
                IEqualityComparer<TProperty> comparer = null )
            {
                if( getProperty.NullReference() )
                    throw Exc.Null(nameof(getProperty));

                string propertyName = GetMemberExpression(getProperty.Body).Member.Name; // memberExpression.Member should be a PropertyInfo
                var func = getProperty.Compile();
                return Property(propertyName, func, comparer);
            }

            private static MemberExpression GetMemberExpression( Expression expression )
            {
                if( expression is MemberExpression memberExpression )
                    return memberExpression;
                else if( expression is UnaryExpression unaryExpression ) // maybe you're trying to get a field? (could also be ExpressionType.Convert or ExpressionType.ConvertChecked)
                    return GetMemberExpression(unaryExpression.Operand);
                else
                    throw new ArgumentException($"Failed to parse expression tree!");
            }

            #endregion

            #region OnChange

            /// <summary>
            /// Specifies the delegate to invoke, when the value of the last property in the chain changes.
            /// If there is no comparer for that property, then this gets called for each <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
            /// </summary>
            /// <param name="onChange">The delegate to invoke, when the value of the last property in the chain changes. The first parameter is the old value, and the second the new one. They may be the same, unless you have a comparer for the last property.</param>
            /// <returns>The new <see cref="PropertyChangedChain"/> instance created.</returns>
            public PropertyChangedChain OnChange( Action<TSource, TSource> onChange )
            {
                if( this.chain.Count == 0 )
                    throw new InvalidOperationException("Please specify a property to detect a change for beforehand!");

                var newLink = new ChainChangeNotifier<TSource>(onChange);

                if( this.chain.Count != 0 )
                {
                    var lastLink = (IChainLinkConnector<TSource>)this.chain.Last();
                    lastLink.SetNextLink(newLink);
                }
                this.chain.Add(newLink);

                return new FullChain<TStart>(this.chain, this.start);
            }

            /// <summary>
            /// Specifies the delegate to invoke, when the value of the last property in the chain changes.
            /// If there is no comparer for that property, then this gets called for each <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
            /// </summary>
            /// <param name="onChange">The delegate to invoke, when the value of the last property in the chain changes.</param>
            /// <returns>The new <see cref="PropertyChangedChain"/> instance created.</returns>
            public PropertyChangedChain OnChange( Action onChange )
            {
                if( onChange.NullReference() )
                    throw Exc.Null(nameof(onChange));

                return this.OnChange(( oldValue, newValue ) => onChange());
            }

            #endregion
        }

        #endregion

        #region FullChain

        private class FullChain<TStart> : PropertyChangedChain
        {
            internal static readonly bool SourceMayBeNull = CanBeAssignedNull<TStart>();
            internal static readonly bool IsRefType = IsReferenceType<TStart>();
            internal static readonly bool ImplementsINotifyPropertyChanged = ImplementsInterface<TStart, INotifyPropertyChanged>();

            private readonly List<object> chainLinks;
            private readonly TStart source;

            internal FullChain( List<object> links, TStart start )
                : base()
            {
                this.chainLinks = links ?? throw Exc.Null(nameof(links));
                this.source = start;

                this.FirstLink.Initialize(this.source); // will propagate to other links
            }

            protected override void OnDispose()
            {
                foreach( var link in this.chainLinks )
                {
                    if( link is IDisposable disposable )
                        disposable.Dispose();
                }
                this.chainLinks.Clear();
            }

            private IChainLink<TStart> FirstLink => ((IChainLink<TStart>)this.chainLinks[0]);

            private void Simulate()
            {
                this.FirstLink.OnParentPropertyValueChanged(this.source, this.source, forceChange: true);
            }

            public override void SimulatePropertyChange()
            {
                this.ThrowIfDisposed();

                if( !UI.InvokeRequired )
                    this.Simulate();
                else
                    UI.BeginInvoke(this.Simulate);
            }
        }

        #endregion

        private PropertyChangedChain()
        {
        }

        /// <summary>
        /// Reads the current property values, and then executes the delegate, whether the final value changed or not.
        /// Ignores comparers.
        /// </summary>
        public abstract void SimulatePropertyChange();

        /// <summary>
        /// Helps creating a <see cref="PropertyChangedChain"/> instance.
        /// </summary>
        /// <typeparam name="TStart">The type of the first object in the chain, from which to get the first property of.</typeparam>
        /// <returns>An object that helps creating a <see cref="PropertyChangedChain"/>, using it's fluent interface.</returns>
        public static Builder<TStart, TStart> Start<TStart>( TStart source )
        {
            if( source == null )
                throw Exc.Null(nameof(source));

            return new Builder<TStart, TStart>(new List<object>(), source);
        }
    }
}
