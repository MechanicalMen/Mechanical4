using System;
using Mechanical4.MVVM;

namespace Mechanical4.Tests.MVVM
{
    internal class FakeUIThread : IUIThread
    {
        public bool IsOnUIThread => true;

        public void BeginInvoke( Action action )
        {
            action();
        }
    }
}
