using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using PostEdge.Weaver.Extensions;
using PostSharp.Reflection;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Transformations {
    internal sealed class NotifyPropertyChangedStructuralTransformation : MemberIntroductionTransformation {
        private Assets _assets;
        public NotifyPropertyChangedStructuralTransformation(AspectWeaver aspectWeaver)
            : base(aspectWeaver) {
            var module = aspectWeaver.AspectInfrastructureTask.Project.Module;
            _assets = new Assets(module);
        }
        public override string GetDisplayName(MethodSemantics semantic) {
            var sb = new StringBuilder("Changes structure to implement INotifyPropertyChanged.");
            return sb.ToString();
        }

        public StructuralTransformationInstance CreateInstance(IType type, AspectWeaverInstance aspectWeaverInstance) {
            return new Instance(type, this, aspectWeaverInstance);
        }

        private class Instance : StructuralTransformationInstance<NotifyPropertyChangedStructuralTransformation> {
            private readonly IType _type;
            private readonly TypeDefDeclaration _typeDef;
            public Instance(IType type, NotifyPropertyChangedStructuralTransformation parent, AspectWeaverInstance aspectWeaverInstance)
                : base(parent, aspectWeaverInstance) {
                _type = type;
                _typeDef = type.GetTypeDefinition();
            }

            public Assets Assets {
                get { return Transformation._assets; }
            }

            public override void Implement(TransformationContext context) {
                EnsureHasPropertyChangedEvent(context);
                EnsureImplementsINotifyPropertyChanged(context);
                EnsureOnPropertyChanged(context);
            }

            public override string GetDisplayName(MethodSemantics semantic) {
                return base.GetDisplayName(semantic);
            }

            private void EnsureImplementsINotifyPropertyChanged(TransformationContext context) {
                var iNpcSignature = Assets.INotifyPropertyChangedTypeSignature;
                AddSymJoinPoint(_typeDef, MethodSemantics.None, context.Ordinal);
                if (!_type.ImplementsInterface(iNpcSignature)) {
                    _typeDef.InterfaceImplementations.Add(iNpcSignature);
                }
            }

            private void EnsureHasPropertyChangedEvent(TransformationContext context) {
                var propertyChangedEvent = _typeDef.FindEvent("PropertyChanged");
                if (propertyChangedEvent == null) {
                    var eventDecl = new EventDeclaration {
                        Name = "PropertyChanged",
                        EventType = Assets.PropertyChangedEventHandlerTypeSignature,
                    };
                    _typeDef.Events.Add(eventDecl);
                    Transformation.IntroduceEvent(context, _typeDef, eventDecl);
                }
            }

            private void EnsureOnPropertyChanged(TransformationContext context) {

            }            
        }

        private sealed class Assets {
            public readonly ITypeSignature INotifyPropertyChangedTypeSignature;
            //public readonly GenericEventReference PropertyChangedEvent;
            public readonly ITypeSignature PropertyChangedEventHandlerTypeSignature;
            public Assets(ModuleDeclaration module) {
                if (module == null) throw new ArgumentNullException("module");
                Contract.EndContractBlock();

                INotifyPropertyChangedTypeSignature = module.FindType(typeof(INotifyPropertyChanged));
                //PropertyChangedEvent = 
                //    INotifyPropertyChangedTypeSignature.GetTypeDefinition().FindEvent("PropertyChanged");
                PropertyChangedEventHandlerTypeSignature =
                    module.FindType(typeof(INotifyPropertyChanged).GetEvent("PropertyChanged").EventHandlerType);

            }
        }
    }
}