using Mechanical4.MVVM;
using Mechanical4.Tests.MVVM;
using NUnit.Framework;

namespace Mechanical4.Tests
{
    [SetUpFixture]
    public class AssemblySetup
    {
        //// one-time, asssembly wide

        [OneTimeSetUp]
        public void SetUp()
        {
            UI.Set(new FakeUIThread());
        }

        /*[OneTimeTearDown]
        public void TearDown()
        {
        }*/
    }
}
