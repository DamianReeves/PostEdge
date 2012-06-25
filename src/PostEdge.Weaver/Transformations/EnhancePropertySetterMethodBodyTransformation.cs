using System;
using System.Text;
using PostEdge.Aspects.Dependencies;
using PostEdge.Weaver.CodeModel;
using PostSharp.Aspects.Dependencies;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectInfrastructure.Dependencies;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Dependencies;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;

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
                var methodWrapper = new EnhancePropertySetterMethodBodyWrappingImplementation(_transformationContext, AspectWeaver.AspectInfrastructureTask, context);
                methodWrapper.Execute();
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic) {
                return MethodBodyTransformationOptions.None;
            }            
        }        
    }
}