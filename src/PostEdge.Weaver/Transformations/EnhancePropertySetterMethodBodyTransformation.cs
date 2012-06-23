using System;
using System.Text;
using PostEdge.Aspects.Dependencies;
using PostEdge.Weaver.CodeModel;
using PostEdge.Weaver.Extensions;
using PostSharp.Aspects.Dependencies;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectInfrastructure.Dependencies;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Dependencies;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;

namespace PostEdge.Weaver.Transformations {
    internal sealed class EnhancePropertySetterMethodBodyTransformation : MethodBodyTransformation {
        public EnhancePropertySetterMethodBodyTransformation(AspectWeaver aspectWeaver)
            : base(aspectWeaver) {

            this.Effects.Add(PostEdgeStandardEffects.RaisesPropertyChangedEvent);
            this.Dependencies.Add(
                new AspectDependency(
                    AspectDependencyAction.Order,
                    AspectDependencyPosition.Before,
                    new AndDependencyCondition(
                        new AspectEffectDependencyCondition(StandardEffects.ChangeControlFlow)
                        )
                    )
                );
            this.Dependencies.Add(
                new AspectDependency(
                    AspectDependencyAction.Order,
                    AspectDependencyPosition.Before,
                    new AndDependencyCondition(
                        new AspectEffectDependencyCondition(PostEdgeStandardEffects.GuardPropertyEquality)
                        )
                    )
                );
            this.Dependencies.Add(
                new AspectDependency(
                    AspectDependencyAction.Commute,
                    AspectDependencyPosition.Before,
                    new AndDependencyCondition(
                        new SameAspectInstanceDependencyCondition()
                        )
                    )
                );
        }

        public override string GetDisplayName(MethodSemantics semantic) {
            var sb = new StringBuilder("Invokes PropertyChanged event");
            return sb.ToString();
        }

        public AspectWeaverTransformationInstance CreateInstance(EnhanceSetterTransformationContext transformationContext, AspectWeaverInstance aspectWeaverInstance) {
            return new Instance(transformationContext, this, aspectWeaverInstance);
        }

        private class Instance : MethodBodyTransformationInstance {
            private readonly EnhanceSetterTransformationContext _transformationContext;
            public Instance(EnhanceSetterTransformationContext transformationContext, MethodBodyTransformation parent, AspectWeaverInstance aspectWeaverInstance)
                : base(parent, aspectWeaverInstance) {
                if (transformationContext == null) throw new ArgumentNullException("transformationContext");
                _transformationContext = transformationContext;
            }


            public override void Implement(MethodBodyTransformationContext context) {
                var methodWrapper = new SetterMethodBodyWrappingImplementation(_transformationContext, this.AspectWeaver.AspectInfrastructureTask, context);
                methodWrapper.Execute();
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic) {
                return MethodBodyTransformationOptions.None;
            }            
        }

        private class SetterMethodBodyWrappingImplementation : MethodBodyWrappingImplementation {
            private readonly EnhanceSetterTransformationContext _transformationContext;
            private readonly ITypeSignature _stringTypeSignature;

            public SetterMethodBodyWrappingImplementation(EnhanceSetterTransformationContext transformationContext, AspectInfrastructureTask task, MethodBodyTransformationContext context) 
                : base(task, context) {
                if (transformationContext == null) throw new ArgumentNullException("transformationContext");
                _transformationContext = transformationContext;
                _stringTypeSignature = _transformationContext.Module.Cache.GetIntrinsic(typeof (string));
            }

            public void Execute() {
                Implement(false, true, false, null);
                Context.AddRedirection(Redirection);
            }

            protected override void ImplementOnException(InstructionBlock block, ITypeSignature exceptionType, InstructionWriter writer) {
                
            }

            protected override void ImplementOnExit(InstructionBlock block, InstructionWriter writer) {                
            }

            protected override void ImplementOnSuccess(InstructionBlock block, InstructionWriter writer) {
                var onPropertyChangedMethod = FindOnPropertyChangedWithStringParameter();
                if (onPropertyChangedMethod == null) return;

                var sequence = block.AddInstructionSequence(null, NodePosition.After, null);
                writer.AttachInstructionSequence(sequence);
                //writer.EmitInstructionString();
                writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                writer.EmitInstructionString(OpCodeNumber.Ldstr, _transformationContext.Property.Name);
                //writer.EmitInstructionLocalVariable();
                if (onPropertyChangedMethod.IsVirtual) {
                    writer.EmitInstructionMethod(OpCodeNumber.Callvirt, onPropertyChangedMethod);
                } else {
                    writer.EmitInstructionMethod(OpCodeNumber.Call, onPropertyChangedMethod);
                }
                writer.DetachInstructionSequence();
            }

            protected override void ImplementOnEntry(InstructionBlock block, InstructionWriter writer) {
                //TODO: Move the equality guard creation code to here... we can then greatly refactor the code
            }

            private IGenericMethodDefinition FindOnPropertyChangedWithStringParameter() {
                var invoker = _transformationContext.Module.FindMethod(
                    _transformationContext.Property.DeclaringType,
                    "OnPropertyChanged",
                    BindingOptions.DontThrowException,
                    methodDef =>
                    methodDef.Parameters.Count == 1
                    && methodDef.Parameters[0].ParameterType.Equals(_stringTypeSignature)
                );
                return invoker;
            }
        }
    }
}