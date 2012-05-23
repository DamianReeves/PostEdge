//#define NoVisitor
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
                    AspectDependencyPosition.After,
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
            public TransformationAssets(ModuleDeclaration module) {
                ObjectEqualsMethod = module.FindMethod(typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) }), BindingOptions.Default);
                LocationBindingTypeSignature = module.FindType(typeof(LocationBinding<>));
                SetValueMethod = module.FindMethod(LocationBindingTypeSignature, "SetValue", x => x.DeclaringType.IsGenericDefinition);
            }
        }

        private sealed class Instance : MethodBodyTransformationInstance {
            public PropertyDeclaration Property { get; private set; }
            public TransformationAssets Assets { get { return _transformation._assets; } }

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
                this.AddSymJoinPoint(context);
                //var writer = context.InstructionBlock.                
#if NoVisitor
                var reader = context.InstructionBlock.MethodBody.CreateInstructionReader(true);
                var rootBlock = context.InstructionBlock.MethodBody.RootInstructionBlock;
                if(rootBlock == null) return;
                rootBlock.MethodBody.InitLocalVariables = true;
                var originalMethodStart = rootBlock.FindFirstInstructionSequence();
                if (originalMethodStart == null) return;
                LocalVariableSymbol oldValueVariable;
                var property = Property;
                if (ShouldCheckEquality()) {
                    //
                    // if (value == this.get_Property)
                    //    return;
                    //

                    oldValueVariable = rootBlock.DefineLocalVariable(property.PropertyType, string.Empty);
                    //var reader = new InstructionReader(rootBlock.MethodBody, true);
                    var sequence = originalMethodStart.ParentInstructionBlock.AddInstructionSequence(
                        null, NodePosition.Before, originalMethodStart);
                    var writer = new InstructionWriter();
                    writer.AttachInstructionSequence(sequence);
                    writer.Box_SetterValueIfNeeded(property);
                    writer.AssignValue_LocalVariable(oldValueVariable,
                        () => writer.Get_PropertyValue(property));
                    writer.Box_LocalVariableIfNeeded(oldValueVariable);
                    var isPrimitive = property.PropertyType.IsPrimitive();
                    if (isPrimitive) {
                        writer.Compare_Primitives();
                    } else {
                        writer.Compare_Objects(Assets.ObjectEqualsMethod);
                    }
                    writer.Leave_IfTrue(context.LeaveBranchTarget);
                    writer.DetachInstructionSequence();
                }
#else
                context.InstructionBlock.MethodBody.Visit(new IMethodBodyVisitor[] { new Visitor(this, context) });
#endif
            }

            private bool ShouldCheckEquality() {
                return _transformation._tranformationOptions.Any(x => x.CheckEquality);
            }

            private sealed class Visitor : IMethodBodyVisitor {
                private readonly Instance _instance;
                private readonly MethodBodyTransformationContext _context;
                private bool _isFirstInstruction = true;

                public TransformationAssets Assets { get { return _instance.Assets; } }
                public bool IsFirstInstruction {
                    get { return _isFirstInstruction; }
                    private set { _isFirstInstruction = value; }
                }

                public Visitor(Instance instance, MethodBodyTransformationContext context) {
                    _instance = instance;
                    _context = context;
                }
                public void EnterInstructionBlock(InstructionBlock instructionBlock, InstructionBlockExceptionHandlingKind exceptionHandlingKind) {
                    
                }
                public void LeaveInstructionBlock(InstructionBlock instructionBlock, InstructionBlockExceptionHandlingKind exceptionHandlingKind) { }
                public void EnterInstructionSequence(InstructionSequence instructionSequence) { }
                public void LeaveInstructionSequence(InstructionSequence instructionSequence) { }
                public void EnterExceptionHandler(ExceptionHandler exceptionHandler) { }
                public void LeaveExceptionHandler(ExceptionHandler exceptionHandler) { }

                public void VisitInstruction(InstructionReader instructionReader) {
                    try {
                        if (!IsFirstInstruction) return;
                        var methodBody = instructionReader.CurrentInstructionSequence.MethodBody;                        
                        bool isLocationBinding = methodBody.Method.Name == "SetValue";
                        if (_instance.ShouldCheckEquality()) {
                            WeaveEqualityCheck(_context, instructionReader, _instance.Property, isLocationBinding);
                        }
                    } finally {
                        IsFirstInstruction = false;
                    }
                }

                private void WeaveEqualityCheck(MethodBodyTransformationContext context, InstructionReader reader, PropertyDeclaration property, bool isLocationBinding) {
                    _context.InstructionBlock.MethodBody.InitLocalVariables = true;
                    var propertyType = property.PropertyType;
                    InstructionSequence before, after, toSplitOn, sequence;
                    reader.CurrentInstructionSequence.SplitAroundReaderPosition(reader, out before, out after);
                    toSplitOn = before ?? reader.CurrentInstructionSequence;
                    var oldValueVariable = reader.CurrentInstructionBlock.DefineLocalVariable(propertyType, string.Empty);
                    var previousSibling = reader.CurrentInstructionBlock.PreviousSiblingBlock;
                    var contextInstructionBlock = context.InstructionBlock;
                    var isSameAsContext = contextInstructionBlock == reader.CurrentInstructionBlock;
                    sequence = reader.CurrentInstructionBlock.AddInstructionSequence(null, NodePosition.Before, toSplitOn);

                    var writer = new InstructionWriter();
                    writer.AttachInstructionSequence(sequence);
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
                    writer.AssignValue_LocalVariable(oldValueVariable,
                        () => writer.Get_PropertyValue(property));
                    writer.Box_LocalVariableIfNeeded(oldValueVariable);
                    var isPrimitive = propertyType.IsPrimitive();
                    if (isPrimitive) {
                        writer.Compare_Primitives();
                    } else {
                        //TODO: Try and use the equality operator when present
                        writer.Compare_Objects(Assets.ObjectEqualsMethod);
                    }
                    writer.Leave_IfTrue(context.LeaveBranchTarget);
                    writer.DetachInstructionSequence();
                }
            }
        }
    }
}