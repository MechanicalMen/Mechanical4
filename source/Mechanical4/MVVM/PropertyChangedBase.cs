using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// Implements <see cref="INotifyPropertyChanged"/>.
    /// Raises events on the UI thread.
    /// </summary>
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        #region Disposable

        /// <summary>
        /// Implements <see cref="INotifyPropertyChanged"/> (and <see cref="IDisposable"/>).
        /// Raises events on the UI thread.
        /// </summary>
        public class Disposable : Mechanical4.Disposable.NonBlockingBase, INotifyPropertyChanged
        {
            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="Disposable"/> class.
            /// </summary>
            public Disposable()
            {
            }

            #endregion

            #region IDisposable

            /// <summary>
            /// Releases managed (IDisposable) resources.
            /// </summary>
            protected override void OnDispose()
            {
                this.PropertyChanged = null;
            }

            #endregion

            #region INotifyPropertyChanged

            /// <summary>
            /// Occurs when a property value changes.
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Protected Methods

            /// <summary>
            /// Raises the <see cref="PropertyChanged"/> event on the UI thread.
            /// Synchronous when called from the UI thread, asynchronous otherwise.
            /// </summary>
            /// <param name="e">Specifies the property that changed.</param>
            protected void RaisePropertyChanged( PropertyChangedEventArgs e )
            {
                if( e.NullReference() )
                    throw new ArgumentNullException().StoreFileLine();

                void Invoke()
                {
                    try
                    {
                        this.PropertyChanged?.Invoke(this, e);
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
            /// Raises the <see cref="PropertyChanged"/> event asynchronously on the UI thread.
            /// </summary>
            /// <param name="e">Specifies the property that changed.</param>
            /// <returns>The <see cref="Task"/> representing the operation.</returns>
            protected Task RaisePropertyChangedAsync( PropertyChangedEventArgs e )
            {
                if( e.NullReference() )
                    throw new ArgumentNullException().StoreFileLine();

                return UI.InvokeAsync(() =>
                {
                    try
                    {
                        this.PropertyChanged?.Invoke(this, e);
                    }
                    catch( Exception ex )
                    {
                        AppBase.EnqueueException(ex);
                    }
                });
            }

            /// <summary>
            /// Raises the <see cref="PropertyChanged"/> event on the UI thread.
            /// Synchronous when called from the UI thread, asynchronous otherwise.
            /// </summary>
            /// <param name="property">The name of the property that changed.</param>
            protected void RaisePropertyChanged( [CallerMemberName] string property = null )
            {
                if( property.NullReference() )
                    throw new ArgumentNullException().StoreFileLine();

                this.RaisePropertyChanged(new PropertyChangedEventArgs(property));
            }

            /// <summary>
            /// Raises the <see cref="PropertyChanged"/> event asynchronously on the UI thread.
            /// </summary>
            /// <param name="property">The name of the property that changed.</param>
            /// <returns>The <see cref="Task"/> representing the operation.</returns>
            protected Task RaisePropertyChangedAsync( [CallerMemberName] string property = null )
            {
                if( property.NullReference() )
                    throw new ArgumentNullException().StoreFileLine();

                return this.RaisePropertyChangedAsync(new PropertyChangedEventArgs(property));
            }

            #endregion
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangedBase"/> class.
        /// </summary>
        public PropertyChangedBase()
        {
        }

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Protected Methods

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event on the UI thread.
        /// Synchronous when called from the UI thread, asynchronous otherwise.
        /// </summary>
        /// <param name="e">Specifies the property that changed.</param>
        protected void RaisePropertyChanged( PropertyChangedEventArgs e )
        {
            if( e.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            void Invoke()
            {
                try
                {
                    this.PropertyChanged?.Invoke(this, e);
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
        /// Raises the <see cref="PropertyChanged"/> event asynchronously on the UI thread.
        /// </summary>
        /// <param name="e">Specifies the property that changed.</param>
        /// <returns>The <see cref="Task"/> representing the operation.</returns>
        protected Task RaisePropertyChangedAsync( PropertyChangedEventArgs e )
        {
            if( e.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            return UI.InvokeAsync(() =>
            {
                try
                {
                    this.PropertyChanged?.Invoke(this, e);
                }
                catch( Exception ex )
                {
                    AppBase.EnqueueException(ex);
                }
            });
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event on the UI thread.
        /// Synchronous when called from the UI thread, asynchronous otherwise.
        /// </summary>
        /// <param name="property">The name of the property that changed.</param>
        protected void RaisePropertyChanged( [CallerMemberName] string property = null )
        {
            if( property.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            this.RaisePropertyChanged(new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event asynchronously on the UI thread.
        /// </summary>
        /// <param name="property">The name of the property that changed.</param>
        /// <returns>The <see cref="Task"/> representing the operation.</returns>
        protected Task RaisePropertyChangedAsync( [CallerMemberName] string property = null )
        {
            if( property.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            return this.RaisePropertyChangedAsync(new PropertyChangedEventArgs(property));
        }

        #endregion
    }
}
