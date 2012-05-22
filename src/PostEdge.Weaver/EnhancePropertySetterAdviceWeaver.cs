using System;
using System.Linq;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver {
    internal class EnhancePropertySetterAdviceWeaver : AdviceWeaver {

        protected override void Initialize() {
            base.Initialize();
            this.RequiresRuntimeInstance = false;
            this.RequiresRuntimeReflectionObject = false;
            var aspectWeaver = this.AspectWeaver;
        }

        protected override AdviceGroup CreateAdviceGroup() {
            return new MyAdviceGroup(this);
        }

        public override bool ValidateAdvice(PostSharp.Sdk.CodeModel.IAnnotationInstance adviseAnnotation) {
            if (adviseAnnotation.TargetElement.GetTokenType() != TokenType.TypeDef) return false;
            return base.ValidateAdvice(adviseAnnotation);
        }

        protected override AdviceGroupingKey GetGroupingKey(IAnnotationInstance adviseAnnotation) {
            var key = base.GetGroupingKey(adviseAnnotation);

            return key;
        }

        private sealed class MyAdviceGroup : AdviceGroup {
            public MyAdviceGroup(AdviceWeaver parent) : base(parent) {
                var annotations = this.Annotations;
            }
            protected override void Initialize() {
                base.Initialize();
                var annotations = this.Annotations;
            }
            public override void ProvideTransformations(AspectWeaverInstance aspectWeaverInstance, AspectWeaverTransformationAdder adder) {
                //adder.AddNullTransformation(aspectWeaverInstance.TargetElement, "Enhance property setters", aspectWeaverInstance.Dependencies);
                AddPropertyTransformations(aspectWeaverInstance, adder);
            }

            private void AddPropertyTransformations(AspectWeaverInstance aspectWeaverInstance, AspectWeaverTransformationAdder adder) {
                var type = aspectWeaverInstance.TargetElement as IType;
                if(type == null) return;
                var typeDef = type.GetTypeDefinition();
                var properties =
                    from property in typeDef.Properties
                    where property.CanWrite
                          && !property.IsStatic
                          && property.DeclaringType != null
                          && property.DeclaringType.Equals(typeDef)
                    select property;
                foreach (var property in properties) {
                    //adder.Add(property, );
                }
            }
        }
    }
}