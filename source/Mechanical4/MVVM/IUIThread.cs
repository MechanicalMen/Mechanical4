using System;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// Executes delegates on the UI thread of the implementing platform.
    /// </summary>
    public interface IUIThread
    {
        /// <summary>
        /// Gets a value indicating whether the calling code is running on the UI thread.
        /// </summary>
        bool IsOnUIThread { get; }

        /// <summary>
        /// Executes the specified <see cref="Action"/> asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        void BeginInvoke( Action action );
    }
}
