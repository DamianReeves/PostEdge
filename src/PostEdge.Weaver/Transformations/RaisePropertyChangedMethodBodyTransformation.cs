using System.Text;
using PostEdge.Aspects.Dependencies;
using PostSharp.Aspects.Dependencies;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectInfrastructure.Dependencies;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Dependencies;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;

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
                AddSymJoinPoint(context);
                var npcBlock = context.InstructionBlock;
                
            }

            public override MethodBodyTransformationOptions GetOptions(MetadataDeclaration originalTargetElement, MethodSemantics semantic) {
                return MethodBodyTransformationOptions.None;
            }
        }        
    }
}