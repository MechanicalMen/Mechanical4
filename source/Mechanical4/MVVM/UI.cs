using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// Executes delegates on the UI thread, assuming there is a UI.
    /// </summary>
    public static class UI
    {
        #region Private Members

        private static IUIThread uiThread = null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfNoThread()
        {
            if( uiThread.NullReference() )
                throw new InvalidOperationException("UI thread is unknown! Use UI.Set to specify it.");
        }

        #endregion

        #region Set, IsAvailable

        /// <summary>
        /// Gets a value indicating whether the UI thread is available (specifically, whether <see cref="UI.Set"/> was called).
        /// </summary>
        public static bool IsAvailable => Interlocked.CompareExchange(ref uiThread, null, comparand: null).NotNullReference();

        /// <summary>
        /// Specifies the UI thread to use.
        /// May only set once at most.
        /// </summary>
        /// <param name="thread">The <see cref="IUIThread"/> to use.</param>
        public static void Set( IUIThread thread )
        {
            if( thread.NullReference() )
                throw Exc.Null(nameof(thread));

            var previousValue = Interlocked.CompareExchange(ref uiThread, thread, comparand: null);
            if( previousValue.NotNullReference() )
                throw new InvalidOperationException("UI already initialized!").StoreFileLine();
        }

        #endregion

        #region InvokeRequired

        /// <summary>
        /// Gets a value indicating whether the calling code is running on the UI thread.
        /// </summary>
        public static bool InvokeRequired
        {
            get
            {
                ThrowIfNoThread();

                return !uiThread.IsOnUIThread;
            }
        }

        #endregion

        #region BeginInvoke

        /// <summary>
        /// Executes the specified <see cref="Action"/> asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public static void BeginInvoke( Action action )
        {
            if( action.NullReference() )
                throw Exc.Null(nameof(action));

            ThrowIfNoThread();

            // NOTE: we're not checking whether we're already on the UI thread!
            //       even if we are, this method is supposed to be asynchronous.
            uiThread.BeginInvoke(action);
        }

        #endregion

        #region InvokeAsync

        /// <summary>
        /// Executes the specified <see cref="Action"/> asynchronously on the UI thread.
        /// </summary>
        /// <param name="func">The delegate to invoke.</param>
        /// <returns>The <see cref="Task"/> representing the operation.</returns>
        public static Task<T> InvokeAsync<T>( Func<T> func )
        {
            if( func.NullReference() )
                throw Exc.Null(nameof(func));

            ThrowIfNoThread();

            var tsc = new TaskCompletionSource<T>();
            uiThread.BeginInvoke(
                () =>
                {
                    try
                    {
                        var result = func();
                        tsc.SetResult(result);
                    }
                    catch( Exception ex )
                    {
                        ex.StoreFileLine();
                        tsc.SetException(ex);
                    }
                });
            return tsc.Task;
        }

        /// <summary>
        /// Executes the specified <see cref="Action"/> asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        /// <returns>The <see cref="Task"/> representing the operation.</returns>
        public static Task InvokeAsync( Action action )
        {
            if( action.NullReference() )
                throw Exc.Null(nameof(action));

            object Wrapper()
            {
                action();
                return null;
            }

            return InvokeAsync<object>((Func<object>)Wrapper);
        }

        #endregion
    }
}
