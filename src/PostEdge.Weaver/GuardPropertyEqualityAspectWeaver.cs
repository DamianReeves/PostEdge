using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostEdge.Weaver.Transformations;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver {
    internal class GuardPropertyEqualityAspectWeaver: AspectWeaver {
        private static readonly AspectConfigurationAttribute defaultConfiguration = new AspectConfigurationAttribute();
        private GuardPropertyEqualityTransformation _transformation;

        public GuardPropertyEqualityAspectWeaver() 
            : base(defaultConfiguration, ReflectionObjectBuilder.Dynamic, MulticastTargets.Property) {
            RequiresRuntimeInstance = false;
            RequiresRuntimeReflectionObject = false;
        }

        public override bool ValidateAspectInstance(AspectInstanceInfo aspectInstanceInfo) {
            if (aspectInstanceInfo.TargetElement.GetTokenType() != TokenType.Property) {
                Message.Write(aspectInstanceInfo.TargetElement, SeverityType.Error, "PE-GPE-0001"
                    , "The Guard Property aspect is not allowed on this type of target.");
                return false;
            }
            return base.ValidateAspectInstance(aspectInstanceInfo);
        }

        protected override void Initialize() {
            base.Initialize();
            _transformation = new GuardPropertyEqualityTransformation(this);
            ApplyWaivedEffects(_transformation);
        }

        protected override AspectWeaverInstance CreateAspectWeaverInstance(AspectInstanceInfo aspectInstanceInfo) {
            return new Instance(this,aspectInstanceInfo);
        }

        private class Instance: AspectWeaverInstance {
            public Instance(AspectWeaver aspectWeaver, AspectInstanceInfo aspectInstanceInfo) 
                : base(aspectWeaver, aspectInstanceInfo) {}
            
            public override void ProvideAspectTransformations(AspectWeaverTransformationAdder adder) {
                var aspectWeaver = (GuardPropertyEqualityAspectWeaver) AspectWeaver;
                var property = (PropertyDeclaration) TargetElement;
                if(aspectWeaver._transformation == null) return;

                adder.Add(property.Setter, aspectWeaver._transformation.CreateInstance(property, this));
            }
        }
    }

    internal sealed class GuardPropertyEqualityTransformationContext : IPropertyTransformationContext {
        public GuardPropertyEqualityTransformationContext(PropertyDeclaration property) {
            if (property == null) throw new ArgumentNullException("property");
            Property = property;
        }

        public PropertyDeclaration Property { get; private set; }
        public ModuleDeclaration Module { get { return Property.Module; } }
    }
}
