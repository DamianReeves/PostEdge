using System;
using PostEdge.Weaver.Extensions;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;

namespace PostEdge.Weaver.Transformations {
    internal class GuardPropertyEqualityMethodBodyWrappingImplementation: MethodBodyWrappingImplementation {
        private readonly IPropertyTransformationContext _transformationContext;
        private readonly TransformationAssets _assets;
        public GuardPropertyEqualityMethodBodyWrappingImplementation(IPropertyTransformationContext transformationContext, AspectInfrastructureTask task, MethodBodyTransformationContext context) 
            : base(task, context) {
            if (transformationContext == null) throw new ArgumentNullException("transformationContext");
            _transformationContext = transformationContext;
            _assets = 
                _transformationContext.Module.Cache.GetItem(
                    () => new TransformationAssets(_transformationContext.Module));
        }

        public TransformationAssets Assets {
            get { return _assets; }
        }

        public PropertyDeclaration Property {
            get { return _transformationContext.Property; }
        }

        public void Execute() {
            Implement(true, false, false, null);
            Context.AddRedirection(Redirection);
        }

        protected override void ImplementOnException(InstructionBlock block, ITypeSignature exceptionType, InstructionWriter writer) {}
        protected override void ImplementOnExit(InstructionBlock block, InstructionWriter writer) {}
        protected override void ImplementOnSuccess(InstructionBlock block, InstructionWriter writer) {}

        protected override void ImplementOnEntry(InstructionBlock block, InstructionWriter writer) {
            //TODO: Move the equality guard creation code to here... we can then greatly refactor the code
            AddGuard(block,writer);
        }

        private void AddGuard(InstructionBlock block, InstructionWriter writer) {
            CommonWeavings.AddPropertyGuard(Property, Context, block,writer);
        }
    }
}