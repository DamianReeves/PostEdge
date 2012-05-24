using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using PostEdge.Weaver.Extensions;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Internals;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectInfrastructure.Dependencies;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Dependencies;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;

namespace PostEdge.Weaver {
    internal sealed class EnhancePropertySetterTransformation : MethodBodyTransformation {
        private readonly MetadataDeclaration _targetElement;
        private readonly IList<TransformationOptions> _tranformationOptions;
        private readonly TransformationAssets _assets;

        public EnhancePropertySetterTransformation(AspectWeaver aspectWeaver,
            IEnumerable<IAnnotationValue> enhancePropertySetters, MetadataDeclaration targetElement)
            : base(aspectWeaver) {
            //Initialize Transformation fields
            _targetElement = targetElement;
            _tranformationOptions = GetTransformationOptions(enhancePropertySetters);
            var module = AspectInfrastructureTask.Project.Module;
            _assets = module.Cache.GetItem(() => new TransformationAssets(module));

            this.Effects.Add("EnhancePropertySetter");
            this.Dependencies.Add(
                new AspectDependency(
                    AspectDependencyAction.Order,
                    AspectDependencyPosition.Before,
                    new OrDependencyCondition(
                        new AspectEffectDependencyCondition(StandardEffects.ChangeControlFlow)
                    )
                )
            );
        }

        public override string GetDisplayName(MethodSemantics semantic) {
            var sb = new StringBuilder("Enhanced property");
            if (semantic == MethodSemantics.Setter) {
                sb.Append(" setter");
            }
            if (_tranformationOptions.Any(x => x.CheckEquality)) {
                sb.Append(" with equality check");
            }
            return sb.ToString();
        }

        public MethodBodyTransformationInstance CreateInstance(PropertyDeclaration property, AspectWeaverInstance aspectWeaverInstance) {
            return new Instance(property, this, aspectWeaverInstance);
        }

        private IList<TransformationOptions> GetTransformationOptions(IEnumerable<IAnnotationValue> enhancePropertySetters) {
            var options =
                from attrib in enhancePropertySetters
                select new TransformationOptions
                {
                    CheckEquality = attrib.NamedArguments.GetRuntimeValue<bool>("CheckEquality")
                };
            return options.ToList();
        }

        private sealed class TransformationOptions {
            public bool CheckEquality { get; set; }
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

        private sealed class Instance : MethodBodyTransformationInstance {
            public PropertyDeclaration Property { get; private set; }
            public TransformationAssets Assets { get { return _transformation._assets; } }
            public ITypeSignature TargetType { get { return (ITypeSignature)_transformation._targetElement; } }

            private readonly EnhancePropertySetterTransformation _transformation;
            public Instance(PropertyDeclaration property, MethodBodyTransformation parent, AspectWeaverInstance aspectWeaverInstance)
                : base(parent, aspectWeaverInstance) {
                _transformation = (EnhancePropertySetterTransformation)parent;
                Property = property;
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic) {
                return MethodBodyTransformationOptions.None;
            }

            public override void Implement(MethodBodyTransformationContext context) {
                AddSymJoinPoint(context);                
                context.InstructionBlock.MethodBody.Visit(new IMethodBodyVisitor[] { new Visitor(this, context) });
            }

            private bool ShouldCheckEquality() {
                return _transformation._tranformationOptions.Any(x => x.CheckEquality);
            }

            private sealed class Visitor : IMethodBodyVisitor {
                private readonly Instance _instance;
                private readonly MethodBodyTransformationContext _context;
                private InstructionBlock _targetBlock;
                public TransformationAssets Assets { get { return _instance.Assets; } }                

                public Visitor(Instance instance, MethodBodyTransformationContext context) {
                    _instance = instance;
                    _context = context;
                }
                public void EnterInstructionBlock(InstructionBlock instructionBlock, InstructionBlockExceptionHandlingKind exceptionHandlingKind) {
                    if(_targetBlock != null) return;
                    _targetBlock = instructionBlock;
                    _context.InstructionBlock.MethodBody.InitLocalVariables = true;
                    var firstChildBlock = _targetBlock.FirstChildBlock;
                    var property = _instance.Property;
                    var propertyType = property.PropertyType;
                    bool isLocationBinding = _targetBlock.MethodBody.Method.Name == "SetValue" 
                        && _targetBlock.MethodBody.Method.DeclaringType.IsDerivedFrom(Assets.LocationBindingTypeSignature.GetTypeDefinition());

                    InstructionBlock equalityCheckBlock = 
                        WeaveEqualityCheck(isLocationBinding, property, propertyType, firstChildBlock);
                }                

                public void LeaveInstructionBlock(InstructionBlock instructionBlock, InstructionBlockExceptionHandlingKind exceptionHandlingKind) { }
                public void EnterInstructionSequence(InstructionSequence instructionSequence) { }
                public void LeaveInstructionSequence(InstructionSequence instructionSequence) { }
                public void EnterExceptionHandler(ExceptionHandler exceptionHandler) { }
                public void LeaveExceptionHandler(ExceptionHandler exceptionHandler) { }

                public void VisitInstruction(InstructionReader instructionReader) {}

                private InstructionBlock WeaveEqualityCheck(bool isLocationBinding, PropertyDeclaration property, ITypeSignature propertyType, InstructionBlock firstChildBlock) {
                    InstructionBlock equalityCheckBlock = null;
                    if (_instance.ShouldCheckEquality()) {
                        var oldValueVariable = _targetBlock.DefineLocalVariable(propertyType, string.Empty);
                        equalityCheckBlock = _targetBlock.AddChildBlock(null, NodePosition.Before, firstChildBlock);
                        equalityCheckBlock.Comment = "Equality Check";
                        var sequence = equalityCheckBlock.AddInstructionSequence(null, NodePosition.Before,
                                                                                 equalityCheckBlock.FindFirstInstructionSequence());

                        var writer = new InstructionWriter();
                        writer.AttachInstructionSequence(sequence);                        
                        if (isLocationBinding) {
                            writer.AssignValue_LocalVariable(oldValueVariable
                                ,()=>writer.Call_MethodOnTarget(property.GetGetter(), 
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
                            //writer.AssignValue_LocalVariable(oldValueVariable,
                            //                                 () => writer.Get_PropertyValue(property));
                        } else {
                            writer.AssignValue_LocalVariable(oldValueVariable,
                                                             () => writer.Get_PropertyValue(property));
                        }
                        if (isLocationBinding) {
                            //On the location binding the value parameter is at psotion 3
                            writer.EmitInstruction(OpCodeNumber.Ldarg_3);
                        } else {
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
                        writer.Leave_IfTrue(_context.LeaveBranchTarget);
                        writer.DetachInstructionSequence();
                    }
                    return equalityCheckBlock;
                }
            }
        }
    }
}