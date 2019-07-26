using System;
using System.Windows.Threading;
using Mechanical4.MVVM;

namespace Mechanical4.Windows.MVVM.WPF
{
    /// <summary>
    /// Implements <see cref="IUIThread"/> using a <see cref="Dispatcher"/>.
    /// </summary>
    public class DispatcherUIThread : IUIThread
    {
        private readonly Dispatcher dispatcher;
        private readonly DispatcherPriority dispatcherPriority;

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherUIThread"/> class.
        /// </summary>
        /// <param name="uiDispatcher">The UI <see cref="Dispatcher"/>.</param>
        /// <param name="priority">The priority, relative to the other pending operations in the <see cref="T:System.Windows.Threading.Dispatcher" /> event queue, to invoke delegates with.</param>
        public DispatcherUIThread( Dispatcher uiDispatcher, DispatcherPriority priority = DispatcherPriority.Normal ) // the priority used by Dispatcher.BeginInvoke(method, args)
        {
            if( !Enum.IsDefined(typeof(DispatcherPriority), priority) )
                throw new ArgumentException($"Unknown dispatcher priority: {priority}!");

            this.dispatcher = uiDispatcher ?? throw Exc.Null(nameof(uiDispatcher));
            this.dispatcherPriority = priority;
        }

        /// <summary>
        /// Gets a value indicating whether the calling code is running on the UI thread.
        /// </summary>
        public bool IsOnUIThread => this.dispatcher.CheckAccess();

        /// <summary>
        /// Executes the specified <see cref="Action"/> asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public void BeginInvoke( Action action ) => this.dispatcher.BeginInvoke(action, this.dispatcherPriority);
    }
}
