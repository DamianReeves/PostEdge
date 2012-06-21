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
            return new Instance(property,this, aspectWeaverInstance);
        }

        private class Instance : MethodBodyTransformationInstance {
            private readonly PropertyDeclaration _property;

            public Instance(PropertyDeclaration property, MethodBodyTransformation parent, AspectWeaverInstance aspectWeaverInstance)
                : base(parent, aspectWeaverInstance) {
                _property = property;
            }

            public override void Implement(MethodBodyTransformationContext context) {
                
                var npcBlock = context.InstructionBlock;
                var methodBody = npcBlock.MethodBody;
                var module = _property.Module;
                var onPropertyChangedMethod = FindOnPropertyChangedWithStringParameter(module);
                
                if(onPropertyChangedMethod == null) return;
                
                AddSymJoinPoint(context);

                var weavingHelper = Transformation.AspectInfrastructureTask.WeavingHelper;
                var returnBlock = methodBody.CreateInstructionBlock();
                var sequence = methodBody.CreateInstructionSequence();
                var returnSequence = methodBody.CreateInstructionSequence();
                npcBlock.AddInstructionSequence(sequence, NodePosition.After, npcBlock.FindFirstInstructionSequence());
                npcBlock.AddInstructionSequence(returnSequence, NodePosition.After, sequence);
                //context.LeaveBranchTarget.Redirect(returnSequence);
                var writer = new InstructionWriter();
                writer.AttachInstructionSequence(sequence);
                //writer.EmitInstructionString();
                writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                writer.EmitInstructionString(OpCodeNumber.Ldstr, _property.Name);                
                //writer.EmitInstructionLocalVariable();
                if(onPropertyChangedMethod.IsVirtual) {
                    writer.EmitInstructionMethod(OpCodeNumber.Callvirt, onPropertyChangedMethod);
                }else {
                    writer.EmitInstructionMethod(OpCodeNumber.Call, onPropertyChangedMethod);
                }
                writer.DetachInstructionSequence();
            }

            private void RedirectReturnTrial(MethodBodyTransformationContext context) {
                AddSymJoinPoint(context);
                var npcBlock = context.InstructionBlock;
                var methodBody = npcBlock.MethodBody;
                var reader = methodBody.CreateInstructionReader(true);
                var weavingHelper = Transformation.AspectInfrastructureTask.WeavingHelper;
                //var returnBlock = methodBody.CreateInstructionBlock();
                var returnSequence = methodBody.CreateInstructionSequence();
                context.LeaveBranchTarget.ParentInstructionBlock.AddInstructionSequence(returnSequence, NodePosition.After, context.LeaveBranchTarget);
                //returnBlock.Comment = "PostEdge New Return Block";
                var writer = new InstructionWriter();
                writer.AttachInstructionSequence(returnSequence);
                writer.EmitInstruction(OpCodeNumber.Nop);
                writer.EmitInstruction(OpCodeNumber.Ret);
                writer.DetachInstructionSequence();

                weavingHelper.RedirectReturnInstructions(reader, writer, context.LeaveBranchTarget.ParentInstructionBlock, returnSequence, null, false);                
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