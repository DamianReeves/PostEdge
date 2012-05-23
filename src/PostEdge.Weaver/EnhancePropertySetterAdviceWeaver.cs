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
            private EnhancePropertySetterTransformation _transformation;
            public MyAdviceGroup(AdviceWeaver parent) : base(parent) {}

            protected override void Initialize() {
                base.Initialize();
                //At this point Annotations is populated
                var master = this.MasterAnnotation;
                if(Annotations.Count <= 0) return;
                _transformation = new EnhancePropertySetterTransformation(this.AdviceWeaver.AspectWeaver,
                    this.Annotations.Select(x=>x.Value), this.Annotations[0].TargetElement);
                this.PrepareTransformation(_transformation);
            }

            public override void ProvideTransformations(AspectWeaverInstance aspectWeaverInstance, AspectWeaverTransformationAdder adder) {
                if (_transformation == null) return;
                var type = aspectWeaverInstance.TargetElement as IType;
                if (type == null) return;
                var typeDef = type.GetTypeDefinition();
                //Get all non-static properties declared on this type
                var properties =
                    from property in typeDef.Properties
                    where property.CanWrite
                          && property.CanRead
                          && !property.IsStatic
                          && property.DeclaringType != null
                          && property.DeclaringType.Equals(typeDef)
                    select property;
                foreach (var property in properties) {
                    adder.Add(property.Setter, _transformation.CreateInstance(property, aspectWeaverInstance));
                }
            }
        }
    }
}