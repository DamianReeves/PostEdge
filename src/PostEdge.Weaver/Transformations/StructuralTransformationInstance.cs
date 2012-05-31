using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;

namespace PostEdge.Weaver.Transformations {
    public abstract class StructuralTransformationInstance<TTransformation> : StructuralTransformationInstance where TTransformation : StructuralTransformation {
        protected StructuralTransformationInstance(StructuralTransformation parent, AspectWeaverInstance aspectWeaverInstance)
            : base(parent, aspectWeaverInstance) { }

        public new TTransformation Transformation {
            get { return (TTransformation)base.Transformation; }
        }
    }
}