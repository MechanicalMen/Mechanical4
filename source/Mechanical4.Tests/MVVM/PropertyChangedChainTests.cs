using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using Mechanical4.MVVM;
using NUnit.Framework;

namespace Mechanical4.Tests.MVVM
{
    [TestFixture]
    public static class PropertyChangedChainTests
    {
        #region Test types

        private class A : INotifyPropertyChanged
        {
            private B b = new B();

            public B B
            {
                get => this.b;
                set
                {
                    this.b = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.B)));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private class B // without INotifyPropertyChanged
        {
            public C C { get; set; } = new C();
        }

        private class C : INotifyPropertyChanged
        {
            private S s1 = new S(1);

            public S S1 // Property, value type
            {
                get => this.s1;
                set
                {
                    this.s1 = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.S1)));
                }
            }

            public S S2 = new S(2); // Field, value type

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private struct S
        {
            internal S( int value ) => this.I = value;

            public int I { get; }

            public override string ToString() => this.I.ToString();
        }

        #endregion

        [Test]
        public static void InvalidArguments()
        {
            // Start
            Assert.Throws<ArgumentNullException>(() => PropertyChangedChain.Start<A>(null));

            // Property( string, Func<...> )
            var start = PropertyChangedChain.Start(new Exception());
            Assert.Throws<ArgumentNullException>(() => start.Property(null, ex => ex.Message));
            Assert.Throws<ArgumentException>(() => start.Property(string.Empty, ex => ex.Message));
            Assert.Throws<ArgumentException>(() => start.Property(" ", ex => ex.Message));
            Assert.Throws<ArgumentException>(() => start.Property(" Message", ex => ex.Message));
            Assert.Throws<ArgumentException>(() => start.Property("Message ", ex => ex.Message));
            Assert.Throws<ArgumentNullException>(() => start.Property("Message", (Func<Exception, string>)null));
            Assert.Throws<ArgumentNullException>(() => start.Property((Expression<Func<Exception, string>>)null));

            // Property( Expression<...>
            Assert.Throws<ArgumentException>(() => start.Property(( Exception ex ) => "str"));
            Assert.Throws<ArgumentException>(() => start.Property(( Exception ex ) => ex.Message + "str"));
            Assert.Throws<ArgumentException>(() => start.Property(( Exception ex ) => new Exception())); // not trying to test for all possible combinations, just most likely typos

            // OnChange
            Assert.Throws<InvalidOperationException>(() => start.OnChange(( _old, _new ) => { }));
            Assert.Throws<InvalidOperationException>(() => start.OnChange(() => { }));
            var property = start.Property(ex => ex.Message);
            Assert.Throws<ArgumentNullException>(() => property.OnChange((Action<string, string>)null));
            Assert.Throws<ArgumentNullException>(() => property.OnChange((Action)null));
        }

        [Test]
        public static void SingleProperty_OnChangeOrSimulation()
        {
            var a = new A();
            var b0 = a.B;
            var b1 = new B();
            bool changeDetected = false;

            void TestPropertyChange( PropertyChangedChain pcc )
            {
                Assert.False(changeDetected); // just making the chain, does not trigger a change
                Assert.AreSame(b0, a.B);
                a.B = b1;
                Assert.True(changeDetected);
                Assert.AreSame(b1, a.B);
            }
            void TestSimulation( PropertyChangedChain pcc )
            {
                changeDetected = false;
                pcc.SimulatePropertyChange();
                Assert.True(changeDetected);
                Assert.AreSame(b1, a.B);
            }

            var chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B) // Expression<...>
                .OnChange(() => changeDetected = true); // no arguments
            TestPropertyChange(chain);
            TestSimulation(chain);
            chain.Dispose();

            changeDetected = false;
            a.B = b0;
            chain = PropertyChangedChain
                .Start(a)
                .Property(nameof(A.B), _a => _a.B) // name + delegate
                .OnChange(() => changeDetected = true); // no arguments
            TestPropertyChange(chain);
            TestSimulation(chain);
            chain.Dispose();


            void ChangeHandler_PropertyChange( B oldValue, B newValue )
            {
                Assert.AreSame(b0, oldValue);
                Assert.AreSame(b1, newValue);
                changeDetected = true;
            }

            changeDetected = false;
            a.B = b0;
            chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B) // Expression<...>
                .OnChange(ChangeHandler_PropertyChange); // with arguments
            TestPropertyChange(chain);
            chain.Dispose();

            changeDetected = false;
            a.B = b0;
            chain = PropertyChangedChain
                .Start(a)
                .Property(nameof(A.B), _a => _a.B) // name + delegate
                .OnChange(ChangeHandler_PropertyChange); // with arguments
            TestPropertyChange(chain);
            chain.Dispose();


            void ChangeHandler_Simulation( B oldValue, B newValue )
            {
                Assert.AreSame(b1, oldValue);
                Assert.AreSame(b1, newValue);
                changeDetected = true;
            }

            changeDetected = false;
            chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B) // Expression<...>
                .OnChange(ChangeHandler_Simulation); // with arguments
            TestSimulation(chain);
            chain.Dispose();

            changeDetected = false;
            chain = PropertyChangedChain
                .Start(a)
                .Property(nameof(A.B), _a => _a.B) // name + delegate
                .OnChange(ChangeHandler_Simulation); // with arguments
            TestSimulation(chain);
            chain.Dispose();
        }

        [Test]
        public static void MultiProperty_OnLastPropertyChangeOrSimulation()
        {
            var a = new A();
            var s1 = a.B.C.S1;
            var s2 = a.B.C.S2;
            bool changeDetected = false;

            void TestPropertyChanged( PropertyChangedChain pcc )
            {
                Assert.False(changeDetected); // just making the chain, does not trigger a change
                Assert.AreEqual(s1, a.B.C.S1);
                a.B.C.S1 = s2;
                Assert.True(changeDetected);
                Assert.AreEqual(s2, a.B.C.S1);
            }
            void TestSimulation( PropertyChangedChain pcc )
            {
                changeDetected = false;
                pcc.SimulatePropertyChange();
                Assert.True(changeDetected);
                Assert.AreEqual(s2, a.B.C.S1);
            }

            var chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B) // Expression<...>
                .Property(b => b.C)
                .Property(c => c.S1)
                .OnChange(() => changeDetected = true); // no arguments
            TestPropertyChanged(chain);
            TestSimulation(chain);
            chain.Dispose();

            changeDetected = false;
            a.B.C.S1 = s1;
            chain = PropertyChangedChain
                .Start(a)
                .Property(nameof(A.B), _a => _a.B) // name + delegate
                .Property(nameof(B.C), b => b.C)
                .Property(nameof(C.S1), c => c.S1)
                .OnChange(() => changeDetected = true); // no arguments
            TestPropertyChanged(chain);
            TestSimulation(chain);
            chain.Dispose();


            void ChangeHandler_PropertyChanged( S oldValue, S newValue )
            {
                Assert.AreEqual(s1, oldValue);
                Assert.AreEqual(s2, newValue);
                changeDetected = true;
            }

            changeDetected = false;
            a.B.C.S1 = s1;
            chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B) // Expression<...>
                .Property(b => b.C)
                .Property(c => c.S1)
                .OnChange(ChangeHandler_PropertyChanged); // with arguments
            TestPropertyChanged(chain);
            chain.Dispose();

            changeDetected = false;
            a.B.C.S1 = s1;
            chain = PropertyChangedChain
                .Start(a)
                .Property(nameof(A.B), _a => _a.B) // name + delegate
                .Property(nameof(B.C), b => b.C)
                .Property(nameof(C.S1), c => c.S1)
                .OnChange(ChangeHandler_PropertyChanged); // with arguments
            TestPropertyChanged(chain);
            chain.Dispose();


            void ChangeHandler_Simulation( S oldValue, S newValue )
            {
                Assert.AreEqual(s2, oldValue);
                Assert.AreEqual(s2, newValue);
                changeDetected = true;
            }

            changeDetected = false;
            chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B) // Expression<...>
                .Property(b => b.C)
                .Property(c => c.S1)
                .OnChange(ChangeHandler_Simulation); // with arguments
            TestSimulation(chain);
            chain.Dispose();

            changeDetected = false;
            chain = PropertyChangedChain
                .Start(a)
                .Property(nameof(A.B), _a => _a.B) // name + delegate
                .Property(nameof(B.C), b => b.C)
                .Property(nameof(C.S1), c => c.S1)
                .OnChange(ChangeHandler_Simulation); // with arguments
            TestSimulation(chain);
            chain.Dispose();
        }

        [Test]
        public static void MultiProperty_OnFirstPropertyChange()
        {
            var a = new A();
            var b0 = a.B;
            var b1 = new B();
            b1.C.S1 = new S(2);
            bool changeDetected = false;

            var chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B)
                .Property(b => b.C)
                .Property(c => c.S1)
                .OnChange(( oldValue, newValue ) =>
                {
                    Assert.AreEqual(1, oldValue.I);
                    Assert.AreEqual(2, newValue.I);
                    changeDetected = true;
                });
            using( chain )
            {
                Assert.False(changeDetected);
                Assert.AreEqual(1, a.B.C.S1.I);
                a.B = b1;
                Assert.True(changeDetected);
                Assert.AreEqual(2, a.B.C.S1.I);
            }
        }

        [Test]
        public static void MultiProperty_ChangeWithoutInterface() // also tests change not at the first or last property
        {
            var a = new A(); // B does not implement INotifyPropertyChanged
            var c1 = a.B.C;
            var c2 = new C();
            c2.S1 = c2.S2;
            bool changeDetected = false;

            var chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B)
                .Property(b => b.C)
                .Property(c => c.S1)
                .OnChange(( oldValue, newValue ) =>
                {
                    Assert.AreEqual(1, oldValue.I);
                    Assert.AreEqual(2, newValue.I);
                    changeDetected = true;
                });
            using( chain )
            {
                Assert.False(changeDetected);
                Assert.AreEqual(1, a.B.C.S1.I);

                a.B.C = c2;
                Assert.False(changeDetected); // no interface, no change
                Assert.AreEqual(2, a.B.C.S1.I);

                chain.SimulatePropertyChange();
                Assert.True(changeDetected);
                Assert.AreEqual(2, a.B.C.S1.I);
            }
        }

        [Test]
        public static void MultiProperty_FieldLink()
        {
            var a = new A();
            bool changeDetected = false;

            var chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B)
                .Property(b => b.C)
                .Property(c => c.S2) // S2 is a public field!
                .OnChange(( oldValue, newValue ) =>
                {
                    Assert.AreEqual(2, oldValue.I);
                    Assert.AreEqual(3, newValue.I);
                    changeDetected = true;
                });
            using( chain )
            {
                Assert.False(changeDetected);
                Assert.AreEqual(2, a.B.C.S2.I);

                a.B.C.S2 = new S(3);
                Assert.False(changeDetected); // we obviously do not detect a field change
                Assert.AreEqual(3, a.B.C.S2.I);

                chain.SimulatePropertyChange();
                Assert.True(changeDetected);
                Assert.AreEqual(3, a.B.C.S2.I);
            }
        }

        [Test]
        public static void MultiProperty_NullLink()
        {
            var a = new A();
            bool changeDetected = false;

            //// final property is reference type (A.B.C1)

            var chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B)
                .Property(b => b.C)
                .OnChange(( oldValue, newValue ) =>
                {
                    Assert.NotNull(oldValue);
                    Assert.Null(newValue);
                    changeDetected = true;
                });
            using( chain )
            {
                Assert.False(changeDetected);
                Assert.NotNull(a.B);

                a.B = null; // from object to null
                Assert.True(changeDetected);
                Assert.Null(a.B);
            }


            changeDetected = false;
            chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B)
                .Property(b => b.C)
                .OnChange(( oldValue, newValue ) =>
                {
                    Assert.Null(oldValue);
                    Assert.NotNull(newValue);
                    changeDetected = true;
                });
            using( chain )
            {
                Assert.False(changeDetected);
                Assert.Null(a.B);

                a.B = new B(); // from null to object
                Assert.True(changeDetected);
                Assert.NotNull(a.B);
            }


            //// final property is value type (A.B.C1.S1)

            changeDetected = false;
            chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B)
                .Property(b => b.C)
                .Property(c => c.S1)
                .OnChange(( oldValue, newValue ) =>
                {
                    Assert.AreEqual(1, oldValue.I);
                    Assert.AreEqual(0, newValue.I); // null turns into default(TProperty)
                    changeDetected = true;
                });
            using( chain )
            {
                Assert.False(changeDetected);
                Assert.NotNull(a.B);

                a.B = null; // from object to null
                Assert.True(changeDetected);
                Assert.Null(a.B);
            }


            changeDetected = false;
            chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B)
                .Property(b => b.C)
                .Property(c => c.S1)
                .OnChange(( oldValue, newValue ) =>
                {
                    Assert.AreEqual(0, oldValue.I);
                    Assert.AreEqual(1, newValue.I);
                    changeDetected = true;
                });
            using( chain )
            {
                Assert.False(changeDetected);
                Assert.Null(a.B);

                a.B = new B(); // from null to object
                Assert.True(changeDetected);
                Assert.NotNull(a.B);
            }
        }

        [Test]
        public static void NoInvocationAfterDisposal()
        {
            var a = new A();
            bool changeDetected = false;

            var chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B)
                .OnChange(() => changeDetected = true);
            chain.Dispose();

            Assert.False(changeDetected);
            a.B = new B();
            Assert.False(changeDetected);

            Assert.Throws<ObjectDisposedException>(() => chain.SimulatePropertyChange());
        }

        [Test]
        public static void Comparer()
        {
            var a = new A();
            var c = a.B.C;
            bool changeDetected = false;

            var chain = PropertyChangedChain
                .Start(a)
                .Property(_a => _a.B)
                .Property(b => b.C)
                .Property(_c => c.S1, comparer: new ParityComparer())
                .OnChange(() => changeDetected = true);

            using( chain )
            {
                Assert.False(changeDetected);
                Assert.AreEqual(1, c.S1.I);
                c.S1 = new S(3); // same parity as before
                Assert.False(changeDetected); // no change detected!
                Assert.AreEqual(3, c.S1.I);

                // simulation overrides comparers
                chain.SimulatePropertyChange();
                Assert.True(changeDetected);

                // different parity
                changeDetected = false;
                c.S1 = new S(4);
                Assert.True(changeDetected);
            }
        }

        private class ParityComparer : IEqualityComparer<S>
        {
            private bool IsEven( S s ) => s.I % 2 == 0;

            public bool Equals( S x, S y )
            {
                return IsEven(x) == IsEven(y);
            }

            public int GetHashCode( S obj )
            {
                throw new NotImplementedException();
            }
        }
    }
}
