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
        public EnhancePropertySetterAdviceWeaver() {
            this.RequiresRuntimeInstance = false;
            this.RequiresRuntimeReflectionObject = false;
        }
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
            private EnhancePropertySetterMethodBodyTransformation _enhancePropertySetterMethodBodyTransformation;

            private EnhanceSetterTransformationOptions _enhanceSetterTransformationOptions;
            public MyAdviceGroup(AdviceWeaver parent) : base(parent) {}

            protected override void Initialize() {
                base.Initialize();
                //At this point Annotations is populated
                _enhanceSetterTransformationOptions = GetTransformationOptions();
                if (_enhanceSetterTransformationOptions == null) return;
                if (_enhanceSetterTransformationOptions.InvokePropertyChanged) {
                    _raisePropertyChangedMethodBodyTransformation = 
                        new RaisePropertyChangedMethodBodyTransformation(AdviceWeaver.AspectWeaver);
                    PrepareTransformation(_raisePropertyChangedMethodBodyTransformation);

                    _enhancePropertySetterMethodBodyTransformation =
                        new EnhancePropertySetterMethodBodyTransformation(AdviceWeaver.AspectWeaver);
                    PrepareTransformation(_enhancePropertySetterMethodBodyTransformation);
                }
                if (_enhanceSetterTransformationOptions.CheckEquality) {
                    _guardEqualityTransformation = 
                        new GuardPropertyEqualityTransformation(AdviceWeaver.AspectWeaver);
                    PrepareTransformation(_guardEqualityTransformation);
                }                
            }

            public override void ProvideTransformations(AspectWeaverInstance aspectWeaverInstance, AspectWeaverTransformationAdder adder) {
                if (_enhanceSetterTransformationOptions == null || !_enhanceSetterTransformationOptions.ShouldTransform()) return;
                var type = aspectWeaverInstance.TargetElement as IType;
                if (type == null) return;
                var typeDef = type.GetTypeDefinition();
                //Get all non-static properties declared on this type
                var method = typeDef.GetMethodsBySignature(_enhanceSetterTransformationOptions.Signatures);
                var properties =
                    from property in typeDef.Properties
                    where property.CanWrite
                          && property.CanRead
                          && !property.IsStatic
                          && property.DeclaringType != null
                          && property.DeclaringType.Equals(typeDef)
                    select property;
                foreach (var property in properties) {
                    var txContext = new EnhanceSetterTransformationContext(property, _enhanceSetterTransformationOptions);
                    //TODO: If NoChangeNotification attribute is specified then skip this transformation
                    adder.Add(property.Setter,
                            _enhancePropertySetterMethodBodyTransformation.CreateInstance(txContext, aspectWeaverInstance));
                }
            }

            private EnhanceSetterTransformationOptions GetTransformationOptions() {
                var annotations =
                    from attrib in Annotations.Select(x => x.Value.ConstructRuntimeObject<EnhancePropertySetterAttribute>())
                    select new EnhanceSetterTransformationOptions {
                        CheckEquality = attrib.CheckEquality,
                        InvokePropertyChanged = attrib.InvokePropertyChanged,
                        Signatures = attrib.PropertyChangedMethodNames.Split(new[]{',',';'},StringSplitOptions.RemoveEmptyEntries)
                    };
                return annotations.FirstOrDefault();
            }            
        }
    }

    internal sealed class EnhanceSetterTransformationContext {
        public EnhanceSetterTransformationContext(PropertyDeclaration property, EnhanceSetterTransformationOptions transformationOptions) {
            if (property == null) throw new ArgumentNullException("property");
            if (transformationOptions == null) throw new ArgumentNullException("transformationOptions");
            Property = property;
            TransformationOptions = transformationOptions;
        }

        public PropertyDeclaration Property { get; private set; }
        public EnhanceSetterTransformationOptions TransformationOptions { get; private set; }
        public ModuleDeclaration Module { get { return Property.Module; } }
    }

    internal sealed class EnhanceSetterTransformationOptions {
        public bool CheckEquality { get; set; }
        public bool InvokePropertyChanged { get; set; }
        public string[] Signatures { get; set; }

        public bool ShouldTransform() {
            return CheckEquality || InvokePropertyChanged;
        }
    }
}