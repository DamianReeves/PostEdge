using System.Linq;
using System.Reflection;
using System.Text;
using PostEdge.Weaver.Extensions;
using PostSharp.Reflection;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;

namespace PostEdge.Weaver.Transformations {
    internal sealed class NotifyPropertyChangedStructuralTransformation : MemberIntroductionTransformation {
        private readonly PropertyNotificationAssets _propertyNotificationAssets;
        public NotifyPropertyChangedStructuralTransformation(AspectWeaver aspectWeaver)
            : base(aspectWeaver) {
            var module = aspectWeaver.AspectInfrastructureTask.Project.Module;
            _propertyNotificationAssets = new PropertyNotificationAssets(module);
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

            public PropertyNotificationAssets PropertyNotificationAssets {
                get { return Transformation._propertyNotificationAssets; }
            }

            public override void Implement(TransformationContext context) {
                EnsureHasPropertyChangedEvent(context);
                EnsureImplementsINotifyPropertyChanged(context);
                EnsurePropertyChangedInvokers(context);

                EnsureHasPropertyChangingEvent(context);
                EnsureImplementsINotifyPropertyChanging(context);
                EnsurePropertyChangingInvokers(context);
            }

            public override string GetDisplayName(MethodSemantics semantic) {
                return base.GetDisplayName(semantic);
            }

            private void EnsureImplementsINotifyPropertyChanged(TransformationContext context) {
                var interfaceSignature = PropertyNotificationAssets.INotifyPropertyChangedTypeSignature;
                AddSymJoinPoint(_typeDef, MethodSemantics.None, context.Ordinal);
                if (!_type.ImplementsInterface(interfaceSignature)) {
                    _typeDef.InterfaceImplementations.Add(interfaceSignature);
                }
            }

            private void EnsureImplementsINotifyPropertyChanging(TransformationContext context) {
                var interfaceSignature = PropertyNotificationAssets.INotifyPropertyChangingTypeSignature;
                AddSymJoinPoint(_typeDef, MethodSemantics.None, context.Ordinal);
                if (!_type.ImplementsInterface(interfaceSignature)) {
                    _typeDef.InterfaceImplementations.Add(interfaceSignature);
                }
            }

            private void EnsureHasPropertyChangedEvent(TransformationContext context) {
                var propertyChangedEvent = _typeDef.FindEvent("PropertyChanged");
                if (propertyChangedEvent == null) {
                    var eventDecl = new EventDeclaration {
                        Name = "PropertyChanged",
                        EventType = PropertyNotificationAssets.PropertyChangedEventHandlerTypeSignature,
                    };
                    _typeDef.Events.Add(eventDecl);
                    Transformation.IntroduceEvent(context, _typeDef, eventDecl);
                }
            }

            private void EnsureHasPropertyChangingEvent(TransformationContext context) {
                var propertyChangedEvent = _typeDef.FindEvent("PropertyChanging");
                if (propertyChangedEvent == null) {
                    var eventDecl = new EventDeclaration {
                        Name = "PropertyChanging",
                        EventType = PropertyNotificationAssets.PropertyChangingEventHandlerTypeSignature,
                    };
                    _typeDef.Events.Add(eventDecl);
                    Transformation.IntroduceEvent(context, _typeDef, eventDecl);
                }
            }

            private void EnsurePropertyChangedInvokers(TransformationContext context) {
                EnsurePropertyNameBasedPropertyChangedInvoker(context);
            }

            private void EnsurePropertyChangingInvokers(TransformationContext context) {
                EnsurePropertyNameBasedPropertyChangingInvoker(context);
            }

            private void EnsurePropertyNameBasedPropertyChangedInvoker(TransformationContext context) {
                var module = _typeDef.Module;
                var invoker = FindOnPropertyChangedWithStringParameter(module);

                if(invoker != null) return;

                EmitOnPropertyChangedMethod(module);
            }

            private void EnsurePropertyNameBasedPropertyChangingInvoker(TransformationContext context) {
                var module = _typeDef.Module;
                var invoker = FindOnPropertyChangingWithStringParameter(module);

                if (invoker != null) return;

                EmitOnPropertyChangingMethod(module);
            }

            private void EmitOnPropertyChangedMethod(ModuleDeclaration module) {
                var propertyChangedField = _typeDef.FindField("PropertyChanged");
                if (propertyChangedField == null
                    || !propertyChangedField.Field.FieldType.Equals(PropertyNotificationAssets.PropertyChangedEventHandlerTypeSignature)
                    || !propertyChangedField.Field.DeclaringType.Equals(_typeDef)
                    ) {
                    //If we can't find the field we won't be able to call the invoker
                    //TODO: We should probably throw or produce a PostSharp Error here at compile time
                    return;
                }

                var method = new MethodDefDeclaration {
                    Attributes = MethodAttributes.Public | MethodAttributes.Virtual,
                    Name = "OnPropertyChanged",
                    CallingConvention = CallingConvention.HasThis,
                };
                _typeDef.Methods.Add(method);
                Transformation.WeavingHelper.AddCompilerGeneratedAttribute(method.CustomAttributes);
                //method.Parameters.EnsureCapacity(1);            
                var parameter = new ParameterDeclaration {
                    Name = "propertyName",
                    ParameterType = module.Cache.GetIntrinsic(typeof (string)),
                };
                method.Parameters.Add(parameter);

                var methodBody = new MethodBodyDeclaration();
                methodBody.EnsureWritableLocalVariables();
                var instructionBlock = methodBody.CreateInstructionBlock();
                var sequence = methodBody.CreateInstructionSequence();
                var endSequence = methodBody.CreateInstructionSequence();

                instructionBlock.AddInstructionSequence(sequence, NodePosition.After, null);
                instructionBlock.AddInstructionSequence(endSequence, NodePosition.After, sequence);
                methodBody.RootInstructionBlock = instructionBlock;
                method.MethodBody = methodBody;
                methodBody.CreateLocalVariable(PropertyNotificationAssets.PropertyChangedEventHandlerTypeSignature);
                methodBody.CreateLocalVariable(module.Cache.GetIntrinsic(typeof (bool)));

                var writer = new InstructionWriter();
                writer.AttachInstructionSequence(sequence);
                //PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
                writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                writer.EmitInstructionField(OpCodeNumber.Ldfld, propertyChangedField.Field);
                writer.EmitInstruction(OpCodeNumber.Stloc_0);

                //bool flag = propertyChanged == null;
                writer.EmitInstruction(OpCodeNumber.Ldloc_0);
                writer.EmitInstruction(OpCodeNumber.Ldnull);
                writer.EmitInstruction(OpCodeNumber.Ceq);
                writer.EmitInstruction(OpCodeNumber.Stloc_1);

                //if(flag)...
                writer.EmitInstruction(OpCodeNumber.Ldloc_1);
                writer.EmitBranchingInstruction(OpCodeNumber.Brtrue_S, endSequence);

                //handler(this, new PropertyChangedEventArgs(propertyName))
                writer.EmitInstruction(OpCodeNumber.Ldloc_0);
                writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                writer.EmitInstruction(OpCodeNumber.Ldarg_1);
                writer.EmitInstructionMethod(OpCodeNumber.Newobj, PropertyNotificationAssets.PropertyChangedEventArgsConstructor);
                writer.EmitInstructionMethod(OpCodeNumber.Callvirt, PropertyNotificationAssets.PropertyChangedEventHandlerInvokeMethod);
                writer.DetachInstructionSequence();

                writer.AttachInstructionSequence(endSequence);
                writer.EmitInstruction(OpCodeNumber.Ret);
                writer.DetachInstructionSequence();
            }

            private void EmitOnPropertyChangingMethod(ModuleDeclaration module) {
                var propertyChangingField = _typeDef.FindField("PropertyChanging");
                if (propertyChangingField == null
                    || !propertyChangingField.Field.FieldType.Equals(PropertyNotificationAssets.PropertyChangingEventHandlerTypeSignature)
                    || !propertyChangingField.Field.DeclaringType.Equals(_typeDef)
                    ) {
                    //If we can't find the field we won't be able to call the invoker
                    //TODO: We should probably throw or produce a PostSharp Error here at compile time
                    return;
                }

                var method = new MethodDefDeclaration {
                    Attributes = MethodAttributes.Public | MethodAttributes.Virtual,
                    Name = "OnPropertyChanging",
                    CallingConvention = CallingConvention.HasThis,
                };
                _typeDef.Methods.Add(method);
                Transformation.WeavingHelper.AddCompilerGeneratedAttribute(method.CustomAttributes);
                //method.Parameters.EnsureCapacity(1);            
                var parameter = new ParameterDeclaration {
                    Name = "propertyName",
                    ParameterType = module.Cache.GetIntrinsic(typeof(string)),
                };
                method.Parameters.Add(parameter);

                var methodBody = new MethodBodyDeclaration();
                methodBody.EnsureWritableLocalVariables();
                var instructionBlock = methodBody.CreateInstructionBlock();
                var sequence = methodBody.CreateInstructionSequence();
                var endSequence = methodBody.CreateInstructionSequence();

                instructionBlock.AddInstructionSequence(sequence, NodePosition.After, null);
                instructionBlock.AddInstructionSequence(endSequence, NodePosition.After, sequence);
                methodBody.RootInstructionBlock = instructionBlock;
                method.MethodBody = methodBody;
                methodBody.CreateLocalVariable(PropertyNotificationAssets.PropertyChangingEventHandlerTypeSignature);
                methodBody.CreateLocalVariable(module.Cache.GetIntrinsic(typeof(bool)));

                var writer = new InstructionWriter();
                writer.AttachInstructionSequence(sequence);
                //PropertyChangingEventHandler propertyChanging = this.PropertyChanging;
                writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                writer.EmitInstructionField(OpCodeNumber.Ldfld, propertyChangingField.Field);
                writer.EmitInstruction(OpCodeNumber.Stloc_0);

                //bool flag = propertyChanging == null;
                writer.EmitInstruction(OpCodeNumber.Ldloc_0);
                writer.EmitInstruction(OpCodeNumber.Ldnull);
                writer.EmitInstruction(OpCodeNumber.Ceq);
                writer.EmitInstruction(OpCodeNumber.Stloc_1);

                //if(flag)...
                writer.EmitInstruction(OpCodeNumber.Ldloc_1);
                writer.EmitBranchingInstruction(OpCodeNumber.Brtrue_S, endSequence);

                //handler(this, new PropertyChangedEventArgs(propertyName))
                writer.EmitInstruction(OpCodeNumber.Ldloc_0);
                writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                writer.EmitInstruction(OpCodeNumber.Ldarg_1);
                writer.EmitInstructionMethod(OpCodeNumber.Newobj, PropertyNotificationAssets.PropertyChangingEventArgsConstructor);
                writer.EmitInstructionMethod(OpCodeNumber.Callvirt, PropertyNotificationAssets.PropertyChangingEventHandlerInvokeMethod);
                writer.DetachInstructionSequence();

                writer.AttachInstructionSequence(endSequence);
                writer.EmitInstruction(OpCodeNumber.Ret);
                writer.DetachInstructionSequence();
            }

            private IGenericMethodDefinition FindOnPropertyChangedWithStringParameter(ModuleDeclaration module) {
                var invoker = module.FindMethod(
                    _typeDef,
                    "OnPropertyChanged",
                    BindingOptions.DontThrowException,
                    methodDef =>
                    methodDef.Parameters.Count == 1
                    && methodDef.Parameters[0].ParameterType
                           .Equals(module.Cache.GetIntrinsic(typeof (string)))
                    );
                return invoker;
            }

            private IGenericMethodDefinition FindOnPropertyChangingWithStringParameter(ModuleDeclaration module) {
                var invoker = module.FindMethod(
                    _typeDef,
                    "OnPropertyChanging",
                    BindingOptions.DontThrowException,
                    methodDef =>
                    methodDef.Parameters.Count == 1
                    && methodDef.Parameters[0].ParameterType
                           .Equals(module.Cache.GetIntrinsic(typeof(string)))
                    );
                return invoker;
            }
        }
    }
}