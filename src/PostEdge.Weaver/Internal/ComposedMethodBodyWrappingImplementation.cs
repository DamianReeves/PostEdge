using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Internal {

    internal abstract class ComposedMethodBodyWrappingImplementation:MethodBodyWrappingImplementation {
        private readonly MethodBodyWrappingImplementationContext _wrappingImplementationContext;

        protected ComposedMethodBodyWrappingImplementation(MethodBodyWrappingImplementationContext wrappingImplementationContext, AspectInfrastructureTask task, MethodBodyTransformationContext context) 
            : base(task, context) {
            if (wrappingImplementationContext == null) throw new ArgumentNullException("wrappingImplementationContext");
            _wrappingImplementationContext = wrappingImplementationContext;
            CompositionInitializer.SatisfyImports(this);
        }

        [Import]
        public Lazy<ExportFactory<IMethodBodyOnEntryImplementation>, IMethodBodyWrappingImplementationMetadata>[] OnEntryImplementations { get; private set; }
        [Import]
        public Lazy<ExportFactory<IMethodBodyOnSuccessImplementation>, IMethodBodyWrappingImplementationMetadata>[] OnSuccessImplementations { get; private set; }

        public MethodBodyWrappingImplementationContext WrappingImplementationContext {
            get { return _wrappingImplementationContext; }
        }

        protected override void ImplementOnException(InstructionBlock block, ITypeSignature exceptionType, InstructionWriter writer) {
            
        }

        protected override void ImplementOnExit(InstructionBlock block, InstructionWriter writer) {
            
        }

        protected override void ImplementOnSuccess(InstructionBlock block, InstructionWriter writer) {

        }

        protected override void ImplementOnEntry(InstructionBlock block, InstructionWriter writer) {
            foreach (var implementation in GetOnEntryImplementations()) {
                using (var factory = implementation.CreateExport()) {
                    var strategy = factory.Value;
                    strategy.ImplementOnEntry(block,writer);
                }
            }
        }

        public virtual void Execute() {
            var ctx = WrappingImplementationContext;
            //Implement this
            Implement(ctx.ImplementOnEntry, ctx.ImplementOnSuccess, ctx.ImplementOnExit
                , ctx.ImplementOnException ? ctx.ExceptionTypes : null);

            if (ctx.CallOriginal) {
                Context.AddRedirection(Redirection);
            }
        }

        protected virtual IEnumerable<ExportFactory<IMethodBodyOnEntryImplementation>> GetOnEntryImplementations() {
            var transformation = WrappingImplementationContext.Transformation;
            return
                from impl in OnEntryImplementations
                where impl.Metadata.Transformation == transformation
                orderby impl.Metadata.Priority
                select impl.Value;
        }

        protected virtual IEnumerable<ExportFactory<IMethodBodyOnSuccessImplementation>> GetOnSuccessImplementations() {
            var transformation = WrappingImplementationContext.Transformation;
            return
                from impl in OnEntryImplementations
                where impl.Metadata.Transformation == transformation
                orderby impl.Metadata.Priority
                select impl.Value;
        }

    }

    internal abstract class MethodBodyWrappingStrategyChain {
        protected abstract IEnumerable<IMethodBodyWrappingStrategy> GetStrategies();
    }
}