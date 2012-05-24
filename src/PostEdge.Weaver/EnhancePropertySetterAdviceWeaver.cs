using System;
using System.Linq;
using System.Reflection;
using PostEdge.Weaver.Extensions;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver {
    internal class EnhancePropertySetterAdviceWeaver : AdviceWeaver {

        protected override void Initialize() {
            base.Initialize();
            RequiresRuntimeInstance = false;
            RequiresRuntimeReflectionObject = false;
        }

        protected override AdviceGroup CreateAdviceGroup() {
            return new MyAdviceGroup(this);
        }

        public override bool ValidateAdvice(IAnnotationInstance adviseAnnotation) {
            if (adviseAnnotation.TargetElement.GetTokenType() != TokenType.TypeDef) return false;
            return base.ValidateAdvice(adviseAnnotation);
        }

        protected override AdviceGroupingKey GetGroupingKey(IAnnotationInstance adviseAnnotation) {
            var key = base.GetGroupingKey(adviseAnnotation);

            return key;
        }

        private sealed class MyAdviceGroup : AdviceGroup {
            private GuardPropertyEqualityTransformation _guardEqualityTransformation;
            public MyAdviceGroup(AdviceWeaver parent) : base(parent) {}

            protected override void Initialize() {
                base.Initialize();
                //At this point Annotations is populated
                if(Annotations.Count <= 0) return;
                if (ShouldCheckEquality()) {
                    _guardEqualityTransformation = new GuardPropertyEqualityTransformation(AdviceWeaver.AspectWeaver);
                }                
                PrepareTransformation(_guardEqualityTransformation);
            }

            private bool ShouldCheckEquality() {
                var results = 
                    from attrib in Annotations.Select(x => x.Value)
                    select new {
                        CheckEquality = attrib.NamedArguments.GetRuntimeValue<bool>("CheckEquality")
                    };
                return results.Any(x=>x.CheckEquality);
            }

            public override void ProvideTransformations(AspectWeaverInstance aspectWeaverInstance, AspectWeaverTransformationAdder adder) {
                if (_guardEqualityTransformation == null) return;
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
                    adder.Add(property.Setter, _guardEqualityTransformation.CreateInstance(property, aspectWeaverInstance));
                }
            }
        }
    }
}