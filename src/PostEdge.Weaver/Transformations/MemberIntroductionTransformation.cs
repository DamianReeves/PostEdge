using System;
using System.Reflection;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Transformations {
    public abstract class MemberIntroductionTransformation:StructuralTransformation {
        protected MemberIntroductionTransformation(AspectWeaver aspectWeaver) : base(aspectWeaver) { }

        protected MethodDefDeclaration IntroduceMethod(TransformationContext context, TypeDefDeclaration type, MethodDefDeclaration methodDef) {
            throw new NotImplementedException();
        }        
    }

    internal sealed class MethodIntroductionOptions {
        public MethodAttributes Attributes { get; set; }
        public bool IsVirtual { get; set; }
    }
}