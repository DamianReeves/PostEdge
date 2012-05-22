using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver {
    internal sealed class EnhancePropertySetterTransformation: MethodBodyTransformation {
        public EnhancePropertySetterTransformation(AspectWeaver aspectWeaver) 
            : base(aspectWeaver) {
            this.Effects.Add("EnhancePropertySetter");
        }

        public override string GetDisplayName(MethodSemantics semantic) {
            return "Enhance property setter";
        }

        public MethodBodyTransformationInstance CreateInstance(AspectWeaverInstance aspectWeaverInstance) {
            return new Instance(this, aspectWeaverInstance);
        }

        private sealed class Instance: MethodBodyTransformationInstance {
            public Instance(MethodBodyTransformation parent, AspectWeaverInstance aspectWeaverInstance) 
                : base(parent, aspectWeaverInstance) {}

            public override void Implement(MethodBodyTransformationContext context) {
                
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic) {
                return MethodBodyTransformationOptions.None;
                ;
            }
        }
    }
}