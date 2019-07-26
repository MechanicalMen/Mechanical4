using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// An <see cref="ICommand"/> that is implemented through delegates.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        #region Private Fields

        private readonly Action execute;
        private readonly Func<bool> canExecute;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// </summary>
        /// <param name="execute">The delegate invoked by Execute.</param>
        /// <param name="canExecute">The delegate invoked by CanExecute.</param>
        public DelegateCommand( Action execute, Func<bool> canExecute = null )
        {
            this.execute = execute ?? throw Exc.Null(nameof(execute));
            this.canExecute = canExecute;
        }

        #endregion

        #region ICommand

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to <c>null</c>.</param>
        /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c>.</returns>
        bool ICommand.CanExecute( object parameter ) => this.CanExecute();

        /// <summary>
        /// The method called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to <c>null</c>.</param>
        void ICommand.Execute( object parameter ) => this.Execute();

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c>.</returns>
        public bool CanExecute() => this.canExecute?.Invoke() ?? true;

        /// <summary>
        /// The method called when the command is invoked.
        /// </summary>
        public void Execute() => this.execute();

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event on the UI thread.
        /// Synchronous when called from the UI thread, asynchronous otherwise.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            void Invoke()
            {
                try
                {
                    this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
                catch( Exception ex )
                {
                    AppBase.EnqueueException(ex);
                }
            }

            if( !UI.InvokeRequired )
                Invoke();
            else
                UI.BeginInvoke(Invoke);
        }

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event asynchronously on the UI thread.
        /// </summary>
        /// <returns>The <see cref="Task"/> representing the operation.</returns>
        public Task RaiseCanExecuteChangedAsync()
        {
            return UI.InvokeAsync(() =>
            {
                try
                {
                    this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
                catch( Exception ex )
                {
                    AppBase.EnqueueException(ex);
                }
            });
        }

        #endregion
    }
}
