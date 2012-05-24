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
        }

        public void EqualityCheckAspect_Can_Be_Applied_To_ReferenceType() {

        }

        public void EqualityCheckAspect_Can_Be_Applied_To_GenericProperty() {

        }

        public class MockEqualityChecked {
            [GuardPropertyEqualityAspect]
            public virtual bool Flag { get; set; }
            public virtual string Name { get; set; }

        }
    }
}