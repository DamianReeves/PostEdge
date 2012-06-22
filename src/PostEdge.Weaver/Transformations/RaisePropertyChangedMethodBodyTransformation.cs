using System.Text;
using PostEdge.Aspects.Dependencies;
using PostSharp.Aspects.Dependencies;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectInfrastructure.Dependencies;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Dependencies;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;

namespace PostEdge.Weaver.Transformations {
    internal sealed class RaisePropertyChangedMethodBodyTransformation : MethodBodyTransformation {
        public RaisePropertyChangedMethodBodyTransformation(AspectWeaver aspectWeaver)
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
            var sb = new StringBuilder("Raises PropertyChanged event");
            return sb.ToString();
        }

        public AspectWeaverTransformationInstance CreateInstance(PropertyDeclaration property, AspectWeaverInstance aspectWeaverInstance) {
            return new Instance(property, this, aspectWeaverInstance);
        }

        private class Instance : MethodBodyTransformationInstance {
            private readonly PropertyDeclaration _property;

            public Instance(PropertyDeclaration property, MethodBodyTransformation parent, AspectWeaverInstance aspectWeaverInstance)
                : base(parent, aspectWeaverInstance) {
                _property = property;
            }

            public override void Implement(MethodBodyTransformationContext context) {
                AddOnPropertyChangedInvocation(context);
            }

            private void AddOnPropertyChangedInvocation(MethodBodyTransformationContext context) {
                var property = _property;
                var propertyType = property.PropertyType;
                var module = _property.Module;
                var onPropertyChangedMethod = FindOnPropertyChangedWithStringParameter(module);
                if(onPropertyChangedMethod == null) return;
                var methodBody = context.InstructionBlock.MethodBody;
                methodBody.InitLocalVariables = true;
                Instruction firstInstruction = null;
                Instruction lastInstruction = null;
                InstructionBlock firstBlockWithInstruction = null;
                InstructionBlock lastBlockWithInstruction = null;
                InstructionSequence firstSequenceWithInstruction = null;
                InstructionSequence lastSequenceWithInstruction = null;
                methodBody.ForEachInstruction(rdr => {
                    if (firstInstruction == null) {
                        firstInstruction = rdr.CurrentInstruction;
                        firstSequenceWithInstruction = rdr.CurrentInstructionSequence;
                        firstBlockWithInstruction = rdr.CurrentInstructionBlock;
                    }
                    lastInstruction = rdr.CurrentInstruction;
                    lastSequenceWithInstruction = rdr.CurrentInstructionSequence;
                    lastBlockWithInstruction = rdr.CurrentInstructionBlock;
                    return true;
                });

                if(lastSequenceWithInstruction == null) return;
                AddSymJoinPoint(context);
                var reader = methodBody.CreateInstructionReader(true);
                reader.JumpToInstructionBlock(lastBlockWithInstruction);
                reader.EnterInstructionSequence(lastSequenceWithInstruction);
                InstructionSequence sequence = null;
                InstructionSequence before = null;
                InstructionSequence after = null;
                while(reader.ReadInstruction()) {
                    if(reader.CurrentInstruction == lastInstruction) {
                        lastSequenceWithInstruction.SplitAroundReaderPosition(reader, out before, out after);
                    }
                }
                if (sequence == null) {
                    sequence = lastBlockWithInstruction.AddInstructionSequence(null, NodePosition.Before,
                                                                               lastSequenceWithInstruction);
                }
                var writer = new InstructionWriter();
                writer.AttachInstructionSequence(sequence);
                //writer.EmitInstructionString();
                writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                writer.EmitInstructionString(OpCodeNumber.Ldstr, _property.Name);
                //writer.EmitInstructionLocalVariable();
                if (onPropertyChangedMethod.IsVirtual) {
                    writer.EmitInstructionMethod(OpCodeNumber.Callvirt, onPropertyChangedMethod);
                } else {
                    writer.EmitInstructionMethod(OpCodeNumber.Call, onPropertyChangedMethod);
                }
                writer.DetachInstructionSequence();
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic) {
                return MethodBodyTransformationOptions.None;
            }

            private IGenericMethodDefinition FindOnPropertyChangedWithStringParameter(ModuleDeclaration module) {
                var invoker = module.FindMethod(
                    _property.DeclaringType,
                    "OnPropertyChanged",
                    BindingOptions.DontThrowException,
                    methodDef =>
                    methodDef.Parameters.Count == 1
                    && methodDef.Parameters[0].ParameterType
                           .Equals(module.Cache.GetIntrinsic(typeof(string)))
                    );
                return invoker;
            }
        }
    }
}