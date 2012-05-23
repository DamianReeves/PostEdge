using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using PostEdge.Weaver.Extensions;
using PostSharp.Aspects.Dependencies;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectInfrastructure.Dependencies;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;

namespace PostEdge.Weaver {
    internal sealed class EnhancePropertySetterTransformation: MethodBodyTransformation {
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
                    new AspectEffectDependencyCondition("ChangeControlFlow")
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
                select new TransformationOptions {
                    CheckEquality = attrib.NamedArguments.GetRuntimeValue<bool>("CheckEquality")
                };
            return options.ToList();
        }

        private sealed class TransformationOptions {
            public bool CheckEquality { get; set; } 
        }

        private sealed class TransformationAssets {
            public IMethod ObjectEqualsMethod { get; private set; }
            public TransformationAssets(ModuleDeclaration module) {
                ObjectEqualsMethod = module.FindMethod(typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) }), BindingOptions.Default);
            }
        }

        private sealed class Instance: MethodBodyTransformationInstance {
            public PropertyDeclaration Property { get; private set; }
            public TransformationAssets Assets { get { return _transformation._assets; } }

            private readonly EnhancePropertySetterTransformation _transformation;
            public Instance(PropertyDeclaration property, MethodBodyTransformation parent, AspectWeaverInstance aspectWeaverInstance) 
                : base(parent, aspectWeaverInstance) {
                _transformation = (EnhancePropertySetterTransformation) parent;
                Property = property;
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic) {
                return MethodBodyTransformationOptions.None;
            }

            public override void Implement(MethodBodyTransformationContext context) { 
                this.AddSymJoinPoint(context);
                //var writer = context.InstructionBlock.
                //context.InstructionBlock.MethodBody.Visit(new IMethodBodyVisitor[] { new Visitor(this, context) });
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
            }  
          
            public bool ShouldCheckEquality() {
                return _transformation._tranformationOptions.Any(x => x.CheckEquality);
            }
            
            private sealed class Visitor: IMethodBodyVisitor {
                private readonly Instance _instance;
                private readonly MethodBodyTransformationContext _context;
                public TransformationAssets Assets { get { return _instance.Assets; } }
                public Visitor(Instance instance, MethodBodyTransformationContext context) {
                    _instance = instance;
                    _context = context;
                }
                public void EnterInstructionBlock(InstructionBlock instructionBlock, InstructionBlockExceptionHandlingKind exceptionHandlingKind) {
                    instructionBlock.MethodBody.InitLocalVariables = true;
                    var originalMethodStart = instructionBlock.FindFirstInstructionSequence();   
                    if(originalMethodStart == null) return;
                    LocalVariableSymbol oldValueVariable;
                    var property = _instance.Property;
                    if (_instance.ShouldCheckEquality()) {
                        //
                        // if (value == this.get_Property)
                        //    return;
                        //

                        oldValueVariable = instructionBlock.DefineLocalVariable(property.PropertyType, string.Empty);                        
                        var sequence = instructionBlock.AddInstructionSequence(null, NodePosition.Before, originalMethodStart);
                        var writer = new InstructionWriter();
                        writer.AttachInstructionSequence(sequence);
                        writer.Box_SetterValueIfNeeded(property);
                        writer.AssignValue_LocalVariable(oldValueVariable,
                            ()=>writer.Get_PropertyValue(property));
                        writer.Box_LocalVariableIfNeeded(oldValueVariable);
                        var isPrimitive = property.PropertyType.IsPrimitive();
                        if (isPrimitive) {
                            writer.Compare_Primitives();
                        } else {
                            writer.Compare_Objects(Assets.ObjectEqualsMethod);
                        }
                        writer.Leave_IfTrue(_context.LeaveBranchTarget);
                        writer.DetachInstructionSequence();
                    }
                }

                public void LeaveInstructionBlock(InstructionBlock instructionBlock, InstructionBlockExceptionHandlingKind exceptionHandlingKind) {
                    
                }

                public void EnterInstructionSequence(InstructionSequence instructionSequence) {}
                public void LeaveInstructionSequence(InstructionSequence instructionSequence) {}

                public void EnterExceptionHandler(ExceptionHandler exceptionHandler) {}

                public void LeaveExceptionHandler(ExceptionHandler exceptionHandler) {}

                public void VisitInstruction(InstructionReader instructionReader) {}
            }
        }
    }
}