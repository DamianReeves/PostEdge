using System.Text;
using PostEdge.Aspects.Dependencies;
using PostEdge.Weaver.Extensions;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Internals;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectInfrastructure.Dependencies;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;

namespace PostEdge.Weaver.Transformations {
    internal sealed class GuardPropertyEqualityTransformation : MethodBodyTransformation {
        private readonly TransformationAssets _assets;

        public GuardPropertyEqualityTransformation(AspectWeaver aspectWeaver)
            : base(aspectWeaver) {
            //Initialize Transformation fields
            var module = AspectInfrastructureTask.Project.Module;
            _assets = module.Cache.GetItem(() => new TransformationAssets(module));

            this.Effects.Add(PostEdgeStandardEffects.GuardPropertyEquality);
            this.Dependencies.Add(
                new AspectDependency(
                    AspectDependencyAction.Order,
                    AspectDependencyPosition.Before,
                    new OrDependencyCondition(
                        new AspectEffectDependencyCondition(StandardEffects.ChangeControlFlow)
                        )
                    )
                );
            this.Dependencies.Add(
                new AspectDependency(
                    AspectDependencyAction.Order,
                    AspectDependencyPosition.After,
                    new OrDependencyCondition(
                        new AspectEffectDependencyCondition(PostEdgeStandardEffects.RaisesPropertyChangedEvent)
                        )
                    )
                );
        }

        public override string GetDisplayName(MethodSemantics semantic) {
            var sb = new StringBuilder("Enhanced property");
            if (semantic == MethodSemantics.Setter) {
                sb.Append(" setter");
            }
            sb.Append(" with equality check");
            return sb.ToString();
        }

        public MethodBodyTransformationInstance CreateInstance(PropertyDeclaration property, AspectWeaverInstance aspectWeaverInstance) {
            return new TransformationInstance(property, this, aspectWeaverInstance);
        }

        private sealed class TransformationAssets {
            public IMethod ObjectEqualsMethod { get; private set; }
            public ITypeSignature LocationBindingTypeSignature { get; private set; }
            public IMethod SetValueMethod { get; private set; }
            public IMethod GetValueMethod { get; private set; }
            public ITypeSignature ObjectTypeSignature { get; private set; }
            public TransformationAssets(ModuleDeclaration module) {
                ObjectTypeSignature = module.FindType(typeof(object));
                ObjectEqualsMethod = module.FindMethod(typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) }), BindingOptions.Default);
                LocationBindingTypeSignature = module.FindType(typeof(LocationBinding<>));
                SetValueMethod = module.FindMethod(LocationBindingTypeSignature, "SetValue", x => x.DeclaringType.IsGenericDefinition);
                GetValueMethod = module.FindMethod(LocationBindingTypeSignature, "GetValue", x => x.DeclaringType.IsGenericDefinition);
            }
        }

        private sealed class TransformationInstance : MethodBodyTransformationInstance {
            public PropertyDeclaration Property { get; private set; }
            public TransformationAssets Assets { get { return _transformation._assets; } }

            private readonly GuardPropertyEqualityTransformation _transformation;
            public TransformationInstance(PropertyDeclaration property, MethodBodyTransformation parent, AspectWeaverInstance aspectWeaverInstance)
                : base(parent, aspectWeaverInstance) {
                _transformation = (GuardPropertyEqualityTransformation)parent;
                Property = property;
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic) {
                return MethodBodyTransformationOptions.None;
            }

            public override void Implement(MethodBodyTransformationContext context) {
                AddSymJoinPoint(context);
                AddGuard(context);
                //context.InstructionBlock.MethodBody.Visit(new IMethodBodyVisitor[] { new Visitor(this, context) });
            }
            private void AddGuard(MethodBodyTransformationContext context) {
                var property = Property;
                var propertyType = property.PropertyType;

                var methodBody = context.InstructionBlock.MethodBody;
                methodBody.InitLocalVariables = true;
                Instruction firstInstruction;
                InstructionBlock firstBlockWithInstruction = null;
                InstructionSequence firstSequenceWithInstruction = null;
                methodBody.ForEachInstruction(rdr => {
                    //if (rdr.CurrentInstruction != null && rdr.CurrentInstruction.OpCodeNumber == OpCodeNumber.Nop)
                    //    return true;
                    firstInstruction = rdr.CurrentInstruction;
                    firstSequenceWithInstruction = rdr.CurrentInstructionSequence;
                    firstBlockWithInstruction = rdr.CurrentInstructionBlock;
                    //firstSequenceWithInstruction.SplitAroundReaderPosition(rdr, out beforeSequence, out afterSequence);
                    return false;
                });

                if(firstSequenceWithInstruction == null) return;
                var beforeSequence = firstBlockWithInstruction
                    .AddInstructionSequence(null, NodePosition.Before,firstSequenceWithInstruction);

                var isLocationBinding = CheckIfIsLocationBinding(methodBody);
                var writer = new InstructionWriter();
                if(beforeSequence != null) {
                    var oldValueVariable = firstBlockWithInstruction
                        .DefineLocalVariable(propertyType, string.Format("old{0}Value",property.Name));

                    writer.AttachInstructionSequence(beforeSequence);
                    if(isLocationBinding) {
                        writer.AssignValue_LocalVariable(oldValueVariable
                            , () => writer.Call_MethodOnTarget(property.GetGetter(),
                                () => {
                                    //Load the instance parameter of the SetValue method
                                    //and convert it to the type
                                    writer.EmitInstruction(OpCodeNumber.Ldarg_1);
                                    //writer.EmitInstructionLoadIndirect(Assets.ObjectTypeSignature);
                                    writer.EmitInstructionType(OpCodeNumber.Ldobj, Assets.ObjectTypeSignature);
                                    writer.EmitConvertFromObject(property.Parent);
                                }
                            )
                        );
                        //On the location binding the value parameter is at psotion 3
                        writer.EmitInstruction(OpCodeNumber.Ldarg_3);
                    }else {
                        writer.AssignValue_LocalVariable(oldValueVariable,
                                                         () => writer.Get_PropertyValue(property));
                        //For a normal property the value parameter is at position 1
                        writer.EmitInstruction(OpCodeNumber.Ldarg_1);
                    }
                    if (propertyType.IsStruct()) {
                        writer.EmitInstructionType(OpCodeNumber.Box, propertyType);
                    }
                    writer.Box_LocalVariableIfNeeded(oldValueVariable);
                    var isPrimitive = propertyType.IsPrimitive();
                    if (isPrimitive) {
                        writer.Compare_Primitives();
                    } else {
                        //TODO: Try and use the equality operator when present
                        writer.Compare_Objects(Assets.ObjectEqualsMethod);
                    }
                    //writer.Leave_IfTrue(_context.LeaveBranchTarget);
                    writer.Leave_IfTrue(context.LeaveBranchTarget);
                    writer.DetachInstructionSequence();
                }

            }
            
            private bool CheckIfIsLocationBinding(MethodBodyDeclaration methodBody) {
                bool isLocationBinding = methodBody.Method.Name == "SetValue"
                        && methodBody.Method.DeclaringType.IsDerivedFrom(Assets.LocationBindingTypeSignature.GetTypeDefinition());
                return isLocationBinding;
            }
        }
    }
}