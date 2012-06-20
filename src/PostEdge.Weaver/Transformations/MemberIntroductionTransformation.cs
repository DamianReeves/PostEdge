using System;
using System.Reflection;
using PostEdge.Weaver.Extensions;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;

namespace PostEdge.Weaver.Transformations {
    public abstract class MemberIntroductionTransformation:StructuralTransformation {
        protected MemberIntroductionTransformation(AspectWeaver aspectWeaver) : base(aspectWeaver) {
        }

        protected MethodDefDeclaration IntroduceMethod(TransformationContext context, TypeDefDeclaration type, MethodDefDeclaration methodDef) {
            throw new NotImplementedException();
        }    
    
        protected void IntroduceEvent(TransformationContext context, TypeDefDeclaration type, EventDeclaration theEvent) {
            var addOn = theEvent.GetAccessor(MethodSemantics.AddOn);
            if (addOn == null) {
                theEvent.ImplementAddOn(type, AspectInfrastructureTask.WeavingHelper);
            }
            var removeOn = theEvent.GetAccessor(MethodSemantics.RemoveOn);
            if (removeOn == null) {
                theEvent.ImplementRemoveOn(type, AspectInfrastructureTask.WeavingHelper);
            }
        }
    }

    internal sealed class MethodIntroductionOptions {
        public MethodAttributes Attributes { get; set; }
        public bool IsVirtual { get; set; }
    }
}