using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using PostSharp.Aspects.Dependencies;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectInfrastructure.Dependencies;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver {
    internal sealed class EnhancePropertySetterTransformation: MethodBodyTransformation {
        private readonly MetadataDeclaration _targetElement;
        private readonly IList<TransformationOptions> _tranformationOptions;
        public EnhancePropertySetterTransformation(AspectWeaver aspectWeaver, 
            IEnumerable<IAnnotationValue> enhancePropertySetters, MetadataDeclaration targetElement) 
            : base(aspectWeaver) {
            _targetElement = targetElement;
            _tranformationOptions = GetTransformationOptions(enhancePropertySetters);
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

        private sealed class Instance: MethodBodyTransformationInstance {
            public PropertyDeclaration Property { get; private set; }
            public Instance(PropertyDeclaration property, MethodBodyTransformation parent, AspectWeaverInstance aspectWeaverInstance) 
                : base(parent, aspectWeaverInstance) {
                Property = property;
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic) {
                return MethodBodyTransformationOptions.None;
            }

            public override void Implement(MethodBodyTransformationContext context) { 
                this.AddSymJoinPoint(context);
                //var writer = context.InstructionBlock.

            }                        
        }
    }
}