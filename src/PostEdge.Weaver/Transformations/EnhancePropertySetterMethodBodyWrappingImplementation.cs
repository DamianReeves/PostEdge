using System;
using PostEdge.Weaver.Extensions;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;

namespace PostEdge.Weaver.Transformations {
    internal class EnhancePropertySetterMethodBodyWrappingImplementation : MethodBodyWrappingImplementation {
        private readonly IPropertyTransformationContext _transformationContext;
        private readonly ITypeSignature _stringTypeSignature;
        private readonly TransformationAssets _assets;
        public EnhancePropertySetterMethodBodyWrappingImplementation(IPropertyTransformationContext transformationContext, AspectInfrastructureTask task, MethodBodyTransformationContext context)
            : base(task, context) {
            if (transformationContext == null) throw new ArgumentNullException("transformationContext");
            _transformationContext = transformationContext;
            _assets =
                _transformationContext.Module.Cache.GetItem(
                    () => new TransformationAssets(_transformationContext.Module));
            _stringTypeSignature = _transformationContext.Module.Cache.GetIntrinsic(typeof(string));
        }

        public TransformationAssets Assets {
            get { return _assets; }
        }

        public PropertyDeclaration Property {
            get { return _transformationContext.Property; }
        }

        public void Execute() {
            Implement(true, true, false, null);
            Context.AddRedirection(Redirection);
        }

        protected override void ImplementOnException(InstructionBlock block, ITypeSignature exceptionType, InstructionWriter writer) {

        }

        protected override void ImplementOnExit(InstructionBlock block, InstructionWriter writer) {
        }

        protected override void ImplementOnSuccess(InstructionBlock block, InstructionWriter writer) {
            RaisePropertyChangedEvent(block, writer);
        }

        protected override void ImplementOnEntry(InstructionBlock block, InstructionWriter writer) {
            //TODO: Move the equality guard creation code to here... we can then greatly refactor the code
            AddGuard(block, writer);
            RaisePropertyChangingEvent(block,writer);
        }

        private void RaisePropertyChangedEvent(InstructionBlock block, InstructionWriter writer) {
            var invokerMethod = FindOnPropertyChangedWithStringParameter();
            CallEventInvokerMethod(invokerMethod, block, writer);
        }

        private void RaisePropertyChangingEvent(InstructionBlock block, InstructionWriter writer) {
            var invokerMethod = FindOnPropertyChangingWithStringParameter();
            CallEventInvokerMethod(invokerMethod, block,writer);
        }

        private void CallEventInvokerMethod(IMethod method, InstructionBlock block, InstructionWriter writer) {
            if (method == null) return;
            if (block.MethodBody.ContainsCallToMethod(method)) return;

            var sequence = block.AddInstructionSequence(null, NodePosition.After, null);
            writer.AttachInstructionSequence(sequence);
            //writer.EmitInstructionString();
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            writer.EmitInstructionString(OpCodeNumber.Ldstr, _transformationContext.Property.Name);
            //writer.EmitInstructionLocalVariable();
            if (method.IsVirtual) {
                writer.EmitInstructionMethod(OpCodeNumber.Callvirt, method);
            } else {
                writer.EmitInstructionMethod(OpCodeNumber.Call, method);
            }
            writer.DetachInstructionSequence();
        }

        private void AddGuard(InstructionBlock block, InstructionWriter writer) {
            CommonWeavings.AddPropertyGuard(Property, Context, block, writer);            
        }

        private IGenericMethodDefinition FindOnPropertyChangedWithStringParameter() {
            var invoker = _transformationContext.Module.FindMethod(
                _transformationContext.Property.DeclaringType,
                "OnPropertyChanged",
                BindingOptions.DontThrowException,
                methodDef =>
                methodDef.Parameters.Count == 1
                && methodDef.Parameters[0].ParameterType.Equals(_stringTypeSignature)
                );
            return invoker;
        }

        private IGenericMethodDefinition FindOnPropertyChangingWithStringParameter() {
            var invoker = _transformationContext.Module.FindMethod(
                _transformationContext.Property.DeclaringType,
                "OnPropertyChanging",
                BindingOptions.DontThrowException,
                methodDef =>
                methodDef.Parameters.Count == 1
                && methodDef.Parameters[0].ParameterType.Equals(_stringTypeSignature)
                );
            return invoker;
        }
    }

    public static class CommonWeavings {
        public static void AddPropertyGuard(PropertyDeclaration property, MethodBodyTransformationContext context, InstructionBlock block, InstructionWriter writer) {
            var propertyType = property.PropertyType;
            var methodBody = block.MethodBody;
            
            var sequence = block.AddInstructionSequence(null, NodePosition.After, null);
            if (sequence == null) return;

            var oldValueVariable =
                block.DefineLocalVariable(propertyType, string.Format("old{0}Value", property.Name));
            var assets = GetTransformationAssets(property.Module);

            writer.AttachInstructionSequence(sequence);
            var isLocationBinding = CheckIfIsLocationBinding(methodBody,assets);
            if (isLocationBinding) {
                writer.AssignValue_LocalVariable(oldValueVariable
                    , () => writer.Call_MethodOnTarget(property.GetGetter()
                        ,
                        () => {
                            //Load the instance parameter of the SetValue method
                            //and convert it to the type
                            writer.EmitInstruction(OpCodeNumber.Ldarg_1);
                            //writer.EmitInstructionLoadIndirect(Assets.ObjectTypeSignature);
                            writer.EmitInstructionType(OpCodeNumber.Ldobj, assets.ObjectTypeSignature);
                            writer.EmitConvertFromObject(property.Parent);
                        }
                    )
                );
                //On the location binding the value parameter is at psotion 3
                writer.EmitInstruction(OpCodeNumber.Ldarg_3);
            } else {
                writer.AssignValue_LocalVariable(oldValueVariable,
                                                    () => writer.Get_PropertyValue(property));
                //For a normal property the value parameter is at position 1
                writer.EmitInstruction(OpCodeNumber.Ldarg_1);
            }
            if (propertyType.IsStruct()) {
                writer.EmitInstructionType(OpCodeNumber.Box, propertyType);
            }
            writer.Box_LocalVariableIfNeeded(oldValueVariable);
            var isPrimitive = propertyType.IsPrimitive();
            if (isPrimitive) {
                writer.Compare_Primitives();
            } else {
                //TODO: Try and use the equality operator when present
                writer.Compare_Objects(assets.ObjectEqualsMethod);
            }
            //writer.Leave_IfTrue(_context.LeaveBranchTarget);
            writer.Leave_IfTrue(context.LeaveBranchTarget);
            writer.DetachInstructionSequence();            
        }

        private static TransformationAssets GetTransformationAssets(ModuleDeclaration module) {
            return module.Cache.GetItem(() => new TransformationAssets(module));
        }

        private static bool CheckIfIsLocationBinding(MethodBodyDeclaration methodBody, TransformationAssets assets) {
            bool isLocationBinding = methodBody.Method.Name == "SetValue"
                                     && methodBody.Method.DeclaringType.IsDerivedFrom(assets.LocationBindingTypeSignature.GetTypeDefinition());
            return isLocationBinding;
        }
    }
}