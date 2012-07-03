using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Internal {

    internal abstract class ComposedMethodBodyWrappingImplementation : MethodBodyWrappingImplementation {
        private readonly MethodBodyWrappingImplementationOptions _wrappingImplementationOptions;

        protected ComposedMethodBodyWrappingImplementation(MethodBodyWrappingImplementationOptions wrappingImplementationOptions, AspectInfrastructureTask task, MethodBodyTransformationContext context)
            : base(task, context) {
            if (wrappingImplementationOptions == null) throw new ArgumentNullException("wrappingImplementationOptions");
            _wrappingImplementationOptions = wrappingImplementationOptions;
            try {
                CompositionInitializer.SatisfyImports(this);
            } catch (Exception ex) {
                throw;
            }
        }

        [ImportMany]
        public ExportFactory<IMethodBodyOnEntryImplementation, IMethodBodyWrappingImplementationMetadata>[] OnEntryImplementations { get; private set; }

        public MethodBodyWrappingImplementationOptions WrappingImplementationOptions {
            get { return _wrappingImplementationOptions; }
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
                    var instance = factory.Value;
                    instance.Initialize(Context);
                    instance.ImplementOnEntry(block, writer);
                }
            }
        }

        public virtual void Execute() {
            var ctx = WrappingImplementationOptions;
            //Implement this
            Implement(ctx.ImplementOnEntry, ctx.ImplementOnSuccess, ctx.ImplementOnExit
                , ctx.ImplementOnException ? ctx.ExceptionTypes : null);

            if (ctx.CallOriginal) {
                Context.AddRedirection(Redirection);
            }
        }

        protected virtual IEnumerable<ExportFactory<IMethodBodyOnEntryImplementation, IMethodBodyWrappingImplementationMetadata>> GetOnEntryImplementations() {
            var transformation = WrappingImplementationOptions.Transformation;
            return
                from impl in OnEntryImplementations
                where impl.Metadata.Transformations != null
                && impl.Metadata.Transformations.Contains(transformation)
                orderby impl.Metadata.Priority
                select impl;
        }
    }
}