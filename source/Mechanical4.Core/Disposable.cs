using System;
using System.Threading;

namespace Mechanical4.Core
{
    /// <summary>
    /// Helps implementing <see cref="IDisposable"/>.
    /// </summary>
    public static class Disposable
    {
        //// NOTE: This class doesn't directly help with unmanaged resources,
        ////       but that's fine, since you likely don't need that. See:
        ////       https://blog.stephencleary.com/2009/08/how-to-implement-idisposable-and.html

        #region InvokeAtMostOnce

        /// <summary>
        /// Invokes the specified delegate at most once.
        /// This class is thread-safe.
        /// Use this class, when you need to implement <see cref="IDisposable"/>,
        /// but are unable to use one of the base classes (since you already inherit from something else).
        /// </summary>
        public sealed class InvokeAtMostOnce
        {
            private Action action;

            /// <summary>
            /// Initializes a new instance of the <see cref="InvokeAtMostOnce"/> class.
            /// </summary>
            /// <param name="dlgt">The delegate to invoke (or <c>null</c>).</param>
            public InvokeAtMostOnce( Action dlgt )
            {
                this.action = dlgt;
            }

            /// <summary>
            /// Invokes the delegate on first call, does nothing afterwards.
            /// </summary>
            /// <returns><c>true</c> if the delegate was executed; otherwise, <c>false</c>.</returns>
            public bool Invoke()
            {
                // - get the delegate from the field
                // - set the field to null
                // - try to invoke the delegate we got from the field
                // - indicate what happened in return value
                var tmp = Interlocked.Exchange(ref this.action, null);
                if( tmp.NotNullReference() )
                {
                    tmp.Invoke();
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// Gets a value indicating whether the next call to <see cref="Invoke"/> will execute the delegate.
            /// </summary>
            public bool NextInvokeExecutes => Interlocked.CompareExchange(ref this.action, null, null).NotNullReference();
        }

        #endregion

        #region IDisposableObject

        /// <summary>
        /// A common interface implemented by the base classes here.
        /// You likely won't need to use it directly.
        /// </summary>
        public interface IDisposableObject : IDisposable
        {
            /// <summary>
            /// Gets a value indicating whether <see cref="IDisposable.Dispose"/> was called.
            /// </summary>
            bool IsDisposed { get; }
        }

        #endregion

        #region NonBlockingBase

        /// <summary>
        /// Implements <see cref="IDisposable"/>.
        /// When Dispose is called concurrently, the first call executes,
        /// while subsequent calls do nothing and return immediately.
        /// This is the default recommended implementation.
        /// </summary>
        public abstract class NonBlockingBase : IDisposableObject
        {
            private readonly InvokeAtMostOnce disposeAction;

            /// <summary>
            /// Initializes a new instance of the <see cref="NonBlockingBase"/> class.
            /// </summary>
            protected NonBlockingBase()
            {
                this.disposeAction = new InvokeAtMostOnce(this.OnDispose);
            }

            /// <summary>
            /// Releases managed (IDisposable) resources.
            /// </summary>
            protected abstract void OnDispose();

            /// <summary>
            /// Disposes this instance.
            /// </summary>
            public void Dispose()
            {
                this.disposeAction.Invoke();
            }

            /// <summary>
            /// Gets a value indicating whether <see cref="Dispose"/> was called.
            /// </summary>
            public bool IsDisposed => !this.disposeAction.NextInvokeExecutes;

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/>, if this instance was already disposed.
            /// </summary>
            protected void ThrowIfDisposed()
            {
                if( this.IsDisposed )
                    throw new ObjectDisposedException(null);
            }
        }

        #endregion

        #region BlockingBase

        /// <summary>
        /// Implements <see cref="IDisposable"/>.
        /// When Dispose is called concurrently, the first call executes,
        /// while subsequent calls block until the first one finishes.
        /// Unless you have a reason to, use <see cref="NonBlockingBase"/> instead.
        /// </summary>
        public abstract class BlockingBase : IDisposableObject
        {
            private readonly InvokeAtMostOnce disposeAction;
            private readonly ManualResetEventSlim mre;

            /// <summary>
            /// Initializes a new instance of the <see cref="BlockingBase"/> class.
            /// </summary>
            protected BlockingBase()
            {
                this.disposeAction = new InvokeAtMostOnce(this.OnDispose);
                this.mre = new ManualResetEventSlim(initialState: false); // nonsignaled
            }

            /// <summary>
            /// Releases managed (IDisposable) resources.
            /// </summary>
            protected abstract void OnDispose();

            /// <summary>
            /// Disposes this instance.
            /// </summary>
            public void Dispose()
            {
                bool disposeInvoked = false; // stops compiler from nagging
                try
                {
                    disposeInvoked = this.disposeAction.Invoke();
                }
                catch
                {
                    disposeInvoked = true; // the only way an exception could have been thrown
                }
                finally
                {
                    if( disposeInvoked )
                        this.mre.Set();
                    else
                        this.mre.Wait();
                }
            }

            /// <summary>
            /// Gets a value indicating whether <see cref="Dispose"/> was called.
            /// </summary>
            public bool IsDisposed => !this.disposeAction.NextInvokeExecutes;

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/>, if this instance was already disposed.
            /// </summary>
            protected void ThrowIfDisposed()
            {
                if( this.IsDisposed )
                    throw new ObjectDisposedException(null);
            }
        }

        #endregion
    }
}
