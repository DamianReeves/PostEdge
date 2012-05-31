using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostEdge.Aspects.Advices;
using PostEdge.Weaver.Extensions;
using PostEdge.Weaver.Transformations;
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
            private RaisePropertyChangedMethodBodyTransformation _raisePropertyChangedMethodBodyTransformation;
            private TransformationOptions _transformationOptions;
            public MyAdviceGroup(AdviceWeaver parent) : base(parent) {}

            protected override void Initialize() {
                base.Initialize();
                //At this point Annotations is populated
                _transformationOptions = GetTransformationOptions();
                if (_transformationOptions == null) return;
                if (_transformationOptions.InvokePropertyChanged) {
                    _raisePropertyChangedMethodBodyTransformation = 
                        new RaisePropertyChangedMethodBodyTransformation(AdviceWeaver.AspectWeaver);
                    PrepareTransformation(_raisePropertyChangedMethodBodyTransformation);
                }
                if (_transformationOptions.CheckEquality) {
                    _guardEqualityTransformation = 
                        new GuardPropertyEqualityTransformation(AdviceWeaver.AspectWeaver);
                    PrepareTransformation(_guardEqualityTransformation);
                }                
            }

            public override void ProvideTransformations(AspectWeaverInstance aspectWeaverInstance, AspectWeaverTransformationAdder adder) {
                if (_transformationOptions == null || !_transformationOptions.ShouldTransform()) return;
                var type = aspectWeaverInstance.TargetElement as IType;
                if (type == null) return;
                var typeDef = type.GetTypeDefinition();
                //Get all non-static properties declared on this type
                var method = typeDef.GetMethodsBySignature(_transformationOptions.Signatures);
                var properties =
                    from property in typeDef.Properties
                    where property.CanWrite
                          && property.CanRead
                          && !property.IsStatic
                          && property.DeclaringType != null
                          && property.DeclaringType.Equals(typeDef)
                    select property;
                foreach (var property in properties) {                    
                    if (_transformationOptions.InvokePropertyChanged) {
                        //TODO: If NoChangeNotification attribute is specified then skip this transformation
                        adder.Add(property.Setter,
                            _raisePropertyChangedMethodBodyTransformation.CreateInstance(property, aspectWeaverInstance));
                    }
                    if (_transformationOptions.CheckEquality) {
                        adder.Add(property.Setter,
                                  _guardEqualityTransformation.CreateInstance(property, aspectWeaverInstance));
                    }                    
                }
            }

            private TransformationOptions GetTransformationOptions() {
                var annotations =
                    from attrib in Annotations.Select(x => x.Value.ConstructRuntimeObject<EnhancePropertySetterAttribute>())
                    select new TransformationOptions {
                        CheckEquality = attrib.CheckEquality,
                        InvokePropertyChanged = attrib.InvokePropertyChanged,
                        Signatures = attrib.PropertyChangedMethodNames.Split(new[]{',',';'},StringSplitOptions.RemoveEmptyEntries)
                    };
                return annotations.FirstOrDefault();
            }

            

            private sealed class TransformationOptions {
                public bool CheckEquality { get; set; }
                public bool InvokePropertyChanged { get; set; }
                public string[] Signatures { get; set; }

                public bool ShouldTransform() {
                    return CheckEquality || InvokePropertyChanged;
                }
            }
        }
    }
}