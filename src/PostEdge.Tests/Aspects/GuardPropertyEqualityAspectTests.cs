using Moq;
using NUnit.Framework;

namespace PostEdge.Aspects {
    public class GuardPropertyEqualityAspectTests {
        [Test]
        public void EqualityCheckAspect_Can_Be_Applied_To_ValueType() {
            var mock = new Mock<MockEqualityChecked> {CallBase = true};
            var underTest = mock.Object;
            underTest.Flag = true;
            underTest.Flag = true;
            mock.VerifyGet(x=>x.Flag, Times.AtLeast(2));
            mock.Verify(x=>x.SetFlag(It.IsAny<bool>()), Times.Once());
        }

        public void EqualityCheckAspect_Can_Be_Applied_To_ReferenceType() {

        }

        public void EqualityCheckAspect_Can_Be_Applied_To_GenericProperty() {

        }

        public class MockEqualityChecked {
            private bool _flag;

            [GuardPropertyEqualityAspect]
            public virtual bool Flag {
                get { return _flag; }
                set { SetFlag(value); }
            }

            public virtual string Name { get; set; }

            public virtual void SetFlag(bool value) {
                _flag = value;
            }
        }
    }
}