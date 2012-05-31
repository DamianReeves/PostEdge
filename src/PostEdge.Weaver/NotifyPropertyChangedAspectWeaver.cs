using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using PostEdge.Weaver.Transformations;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver {
    internal class NotifyPropertyChangedAspectWeaver: AspectWeaver {
        private static readonly AspectConfigurationAttribute DefaultConfiguration = new AspectConfigurationAttribute();
        private NotifyPropertyChangedStructuralTransformation _transformation;
        private Assets _assets;
        public NotifyPropertyChangedAspectWeaver() 
            : base(DefaultConfiguration, ReflectionObjectBuilder.Dynamic, MulticastTargets.Class) {
            RequiresRuntimeInstance = false;
            RequiresRuntimeReflectionObject = false;            
        }

        protected override AspectWeaverInstance CreateAspectWeaverInstance(AspectInstanceInfo aspectInstanceInfo) {
            return new Instance(this,aspectInstanceInfo);
        }

        public override bool ValidateAspectInstance(AspectInstanceInfo aspectInstanceInfo) {
            return base.ValidateAspectInstance(aspectInstanceInfo);
        }

        protected override void Initialize() {
            base.Initialize();
            var module = AspectInfrastructureTask.Project.Module;
            _assets = module.Cache.GetItem(() => new Assets(module));
            _transformation = new NotifyPropertyChangedStructuralTransformation(this);
            ApplyWaivedEffects(_transformation);
        }

        private sealed class Instance: AspectWeaverInstance {
            private readonly NotifyPropertyChangedAspectWeaver _concreteAspectWeaver;
            public Instance(AspectWeaver aspectWeaver, AspectInstanceInfo aspectInstanceInfo) 
                : base(aspectWeaver, aspectInstanceInfo) {
                _concreteAspectWeaver = (NotifyPropertyChangedAspectWeaver) aspectWeaver;
            }

            public override void ProvideAspectTransformations(AspectWeaverTransformationAdder adder) {
                var type = this.TargetElement as IType;
                if(type == null) return;
                var aspectWeaver = _concreteAspectWeaver;
                if (ShouldTransformStructure(type)) {
                    adder.Add(type, aspectWeaver._transformation.CreateInstance(type, this));
                }
            }

            private bool ShouldTransformStructure(IType type) {
                var typeDef = type.GetTypeDefinition();                
                if (!typeDef.InterfaceImplementations.Any(x => x.ImplementedInterface 
                    == _concreteAspectWeaver._assets.INotifyPropertyChangedTypeSignature)) {
                    return true;
                }
                return false;
            }
        }

        private sealed class Assets {
            public readonly ITypeSignature INotifyPropertyChangedTypeSignature;
            public Assets(ModuleDeclaration module) {
                if (module == null) throw new ArgumentNullException("module");
                Contract.EndContractBlock();

                INotifyPropertyChangedTypeSignature =
                    module.FindType(typeof (INotifyPropertyChanged));
            }
        }
    }
}