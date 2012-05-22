using PostSharp.Extensibility;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver {
    internal class SetPropertyAdviceWeaver: GroupingAdviceWeaver {
        protected override AdviceGroup CreateAdviceGroup() {
            throw new System.NotImplementedException();
        }

        private class AdviceGroupInternal: PointcutAwareAdviceGroup {
            private AspectWeaverTransformation _transformation;
            public AdviceGroupInternal(AdviceWeaver parent)
                : base(parent, MulticastTargets.Property, MulticastAttributes.AnyVisibility | MulticastAttributes.AnyScope | MulticastAttributes.AnyVirtuality | MulticastAttributes.AnyGeneration | MulticastAttributes.NonAbstract | MulticastAttributes.Managed | MulticastAttributes.NonLiteral) {                
            }

            protected override void Initialize() {
                base.Initialize();

            }

            protected override void ProvideTransformations(AspectWeaverInstance aspectWeaverInstance, MetadataDeclaration targetElement, AspectWeaverTransformationAdder adder) {
                
            }
        }
    }
}