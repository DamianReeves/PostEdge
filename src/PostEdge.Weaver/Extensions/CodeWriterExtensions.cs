using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;

namespace PostEdge.Weaver.Extensions
{
    internal static class CodeWriter
    {
        #region Mark CompilerGenerated

        public static void MarkCompilerGenerated(this IMetadataDeclaration supportsCustomAttributes, CustomAttributeDeclaration compilerGenerated)
        {
            if (compilerGenerated == null)
                return;

            supportsCustomAttributes.CustomAttributes.Add(compilerGenerated);
        }

        #endregion

        #region Implement Attributes

        public static void ImplementAttributes(this MetadataDeclaration metadata, IEnumerable<CustomAttributeDeclaration> attributesList)
        {
            if (attributesList == null)
                return;

            foreach (var attribute in attributesList
                .Where(attribute => !metadata.CustomAttributes.Contains(attribute)))
            {
                metadata.CustomAttributes.Add(attribute);
            }
        }

        #endregion

        #region Implement Public Property

        public static PropertyDeclaration ImplementPublicProperty(
            this TypeDefDeclaration type,
            string name,
            ITypeSignature propertyType,            
            CustomAttributeDeclaration compilerGenerated)
        {
            return ImplementPublicProperty(type, name, propertyType, 0, null, compilerGenerated);
        }

        public static PropertyDeclaration ImplementPublicProperty(
            this TypeDefDeclaration type,
            string name,
            ITypeSignature propertyType,
            MethodAttributes extraAttributes,
            IEnumerable<CustomAttributeDeclaration> propertyAttributes,
            CustomAttributeDeclaration compilerGenerated)
        {
            var field = CreateBackingField(type, name, propertyType, compilerGenerated);

            var property = CreateProperty(type, name, propertyType, propertyAttributes);

            var methodAttributes = MethodAttributes.Public
                                    | MethodAttributes.HideBySig
                                    | MethodAttributes.SpecialName;

            methodAttributes |= extraAttributes;

            ImplementGetter(property, type, field, methodAttributes, compilerGenerated);
            ImplementSetter(property, type, field, methodAttributes, compilerGenerated);
            return property;
        }        

        private static IField CreateBackingField(
            TypeDefDeclaration type,
            string name,
            ITypeSignature fieldType,
            CustomAttributeDeclaration compilerGenerated)
        {
            var field = new FieldDefDeclaration
            {
                Attributes = FieldAttributes.Private,
                Name = string.Format("<{0}>k__BackingField", name),
                FieldType = fieldType
            };

            // Create a backing field
            type.Fields.Add(field);
            MarkCompilerGenerated(field, compilerGenerated);            
            return GenericHelper.GetCanonicalGenericInstance(field);
        }

        private static PropertyDeclaration CreateProperty(TypeDefDeclaration type, string name, ITypeSignature propertyType, IEnumerable<CustomAttributeDeclaration> attributesList)
        {
            var property = new PropertyDeclaration
            {
                Name = name,
                PropertyType = propertyType,
                CallingConvention = CallingConvention.HasThis,
            };
            type.Properties.Add(property);
            ImplementAttributes(property, attributesList);
            return property;
        }        
        
        private static void ImplementGetter(
            PropertyDeclaration property,
            TypeDefDeclaration type,
            IField field,
            MethodAttributes methodAttributes,
            CustomAttributeDeclaration compilerGenerated)
        {
            // Implement getter
            var getter = new MethodDefDeclaration
            {
                Attributes = methodAttributes,
                Name = "get_" + property.Name,
                CallingConvention = CallingConvention.HasThis,
            };

            type.Methods.Add(getter);

            MarkCompilerGenerated(getter, compilerGenerated);            
            getter.ReturnParameter = new ParameterDeclaration
            {
                ParameterType = property.PropertyType,
                Attributes = ParameterAttributes.Retval
            };

            var methodBody = new MethodBodyDeclaration();
            var sequence = methodBody.CreateInstructionSequence();
            var instructionBlock = methodBody.CreateInstructionBlock();
            instructionBlock.AddInstructionSequence(sequence, NodePosition.After, null);
            methodBody.RootInstructionBlock = instructionBlock;
            getter.MethodBody = methodBody;

            var writer = new InstructionWriter();
            writer.AttachInstructionSequence(sequence);
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            writer.EmitInstructionField(OpCodeNumber.Ldfld, field);
            writer.EmitInstruction(OpCodeNumber.Ret);
            writer.DetachInstructionSequence();

            property.Members.Add(new MethodSemanticDeclaration(MethodSemantics.Getter, getter));
        }

        private static void ImplementSetter(
            PropertyDeclaration property,
            TypeDefDeclaration type,
            IField field,
            MethodAttributes methodAttributes,
            CustomAttributeDeclaration compilerGenerated)
        {
            // Implement setter
            var setter = new MethodDefDeclaration
            {
                Attributes = methodAttributes,
                Name = "set_" + property.Name,
                CallingConvention = CallingConvention.HasThis,
            };
            type.Methods.Add(setter);

            MarkCompilerGenerated(setter, compilerGenerated);
            setter.ReturnParameter = new ParameterDeclaration
            {
                ParameterType = type.Module.Cache.GetIntrinsic(IntrinsicType.Void),
                Attributes = ParameterAttributes.Retval
            };
            setter.Parameters.Add(new ParameterDeclaration(0, "value", property.PropertyType));

            var methodBody = new MethodBodyDeclaration();
            var sequence = methodBody.CreateInstructionSequence();
            var instructionBlock = methodBody.CreateInstructionBlock();
            instructionBlock.AddInstructionSequence(sequence, NodePosition.After, null);
            methodBody.RootInstructionBlock = instructionBlock;
            setter.MethodBody = methodBody;

            var writer = new InstructionWriter();
            writer.AttachInstructionSequence(sequence);
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            writer.EmitInstruction(OpCodeNumber.Ldarg_1);
            writer.EmitInstructionField(OpCodeNumber.Stfld, field);
            writer.EmitInstruction(OpCodeNumber.Ret);
            writer.DetachInstructionSequence();

            property.Members.Add(new MethodSemanticDeclaration(MethodSemantics.Setter, setter));
        }

        #endregion

        #region Implement Public Event
        //private static readonly MethodBase DelegateCombineReflectionMethod = typeof(Delegate).GetMethod()
        public static void ImplementAddOn(
            this EventDeclaration theEvent,
            TypeDefDeclaration type,
            IField field,
            WeavingHelper weavingHelper = null,
            MethodAttributes methodAttributes = 
                MethodAttributes.Public | MethodAttributes.HideBySig
                | MethodAttributes.Virtual | MethodAttributes.Final 
                | MethodAttributes.NewSlot | MethodAttributes.SpecialName) 
        {
            var module = type.Module;
            weavingHelper = weavingHelper ?? new WeavingHelper(module);
            var pEWeavingHelper = module.Cache.GetItem(() => new PostEdgeWeaverAssets(module));

            var method = new MethodDefDeclaration{
                Attributes = methodAttributes,
                Name = "add_" + theEvent.Name,
                CallingConvention = CallingConvention.HasThis
            };
            type.Methods.Add(method);
            weavingHelper.AddCompilerGeneratedAttribute(method.CustomAttributes);
            //method.Parameters.EnsureCapacity(1);            
            var parameter = new ParameterDeclaration {
                Name = "value",
                ParameterType = theEvent.EventType,
            };
            method.Parameters.Add(parameter);
            
            var methodBody = new MethodBodyDeclaration();
            methodBody.EnsureWritableLocalVariables();
            var instructionBlock = methodBody.CreateInstructionBlock();
            var initSequence = methodBody.CreateInstructionSequence();
            var loopSequence = methodBody.CreateInstructionSequence();
            var endSequence = methodBody.CreateInstructionSequence();
            instructionBlock.AddInstructionSequence(initSequence, NodePosition.After, null);
            instructionBlock.AddInstructionSequence(loopSequence, NodePosition.After, initSequence);
            instructionBlock.AddInstructionSequence(endSequence, NodePosition.After, loopSequence);
            methodBody.RootInstructionBlock = instructionBlock;
            method.MethodBody = methodBody;
            methodBody.CreateLocalVariable(theEvent.EventType);
            methodBody.CreateLocalVariable(theEvent.EventType);
            methodBody.CreateLocalVariable(theEvent.EventType);
            methodBody.CreateLocalVariable(module.Cache.GetIntrinsic(typeof (bool)));

            var writer = new InstructionWriter();
            //Initialize
            writer.AttachInstructionSequence(initSequence);

            //PropertyChangedHandler handler = this.PropertyChanged
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);           //Get the this pointer
            writer.EmitInstructionField(OpCodeNumber.Ldfld, field); //Get the event's field
            writer.EmitInstruction(OpCodeNumber.Stloc_0);           //store into the first local variable
            writer.DetachInstructionSequence();

            //Loop
            //do {
            writer.AttachInstructionSequence(loopSequence);            
            
            //PropertyChangedHandler handler2 = handler;
            writer.EmitInstruction(OpCodeNumber.Ldloc_0);
            writer.EmitInstruction(OpCodeNumber.Stloc_1);

            //PropertyChangedHandler handler3 = System.Delegate.Combine(handler2, value)
            writer.EmitInstruction(OpCodeNumber.Ldloc_1);
            writer.EmitInstruction(OpCodeNumber.Ldarg_1);
            writer.EmitInstructionMethod(OpCodeNumber.Call, pEWeavingHelper.DelegateCombineMethod);
            writer.EmitInstructionType(OpCodeNumber.Castclass, theEvent.EventType);
            writer.EmitInstruction(OpCodeNumber.Stloc_2);
            
            //handler = System.Threading.Interlocked.CompareExchnage<PropertyChangedEventHandler>(ref this.PropertyChtyChanged, handler3, handler2);
            var compareExchangeMethodT = pEWeavingHelper.CompareExchangeMethod;
            var compareExchangeMethod = compareExchangeMethodT.GetGenericInstance(module, theEvent.EventType);
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);           //Load this
            writer.EmitInstructionField(OpCodeNumber.Ldflda, field); 
            writer.EmitInstruction(OpCodeNumber.Ldloc_2);
            writer.EmitInstruction(OpCodeNumber.Ldloc_1);
            writer.EmitInstructionMethod(OpCodeNumber.Call, compareExchangeMethod);
            writer.EmitInstruction(OpCodeNumber.Stloc_0);

            
            //flag = handler != handler2;
            writer.EmitInstruction(OpCodeNumber.Ldloc_0);
            writer.EmitInstruction(OpCodeNumber.Ldloc_1);
            writer.EmitInstruction(OpCodeNumber.Ceq);
            writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
            writer.EmitInstruction(OpCodeNumber.Ceq);
            writer.EmitInstruction(OpCodeNumber.Stloc_3);

            //} while (flag);
            writer.EmitInstruction(OpCodeNumber.Ldloc_3);
            writer.EmitBranchingInstruction(OpCodeNumber.Brtrue_S, loopSequence);

            writer.DetachInstructionSequence();

            //End
            writer.AttachInstructionSequence(endSequence);
            writer.EmitInstruction(OpCodeNumber.Ret);               //Exit the method
            writer.DetachInstructionSequence();

            theEvent.Members.Add(new MethodSemanticDeclaration(MethodSemantics.AddOn, method));
        }

        public static void ImplementRemoveOn(
            this EventDeclaration theEvent,
            TypeDefDeclaration type,
            IField field,
            WeavingHelper weavingHelper = null,
            MethodAttributes methodAttributes =
                MethodAttributes.Public | MethodAttributes.HideBySig
                | MethodAttributes.Virtual | MethodAttributes.Final 
                | MethodAttributes.NewSlot | MethodAttributes.SpecialName) 
        {
            var module = type.Module;
            weavingHelper = weavingHelper ?? new WeavingHelper(module);
            var pEWeavingHelper = module.Cache.GetItem(() => new PostEdgeWeaverAssets(module));
    
            var method = new MethodDefDeclaration {
                Attributes = methodAttributes,
                Name = "remove_" + theEvent.Name,
                CallingConvention = CallingConvention.HasThis
            };
            type.Methods.Add(method);
            weavingHelper.AddCompilerGeneratedAttribute(method.CustomAttributes);
            var parameter = new ParameterDeclaration {
                Name = "value",
                ParameterType = theEvent.EventType,
            };
            method.Parameters.Add(parameter);

            var methodBody = new MethodBodyDeclaration();
            methodBody.EnsureWritableLocalVariables();
            var instructionBlock = methodBody.CreateInstructionBlock();
            var initSequence = methodBody.CreateInstructionSequence();
            var loopSequence = methodBody.CreateInstructionSequence();
            var endSequence = methodBody.CreateInstructionSequence();
            instructionBlock.AddInstructionSequence(initSequence, NodePosition.After, null);
            instructionBlock.AddInstructionSequence(loopSequence, NodePosition.After, initSequence);
            instructionBlock.AddInstructionSequence(endSequence, NodePosition.After, loopSequence);
            methodBody.RootInstructionBlock = instructionBlock;
            method.MethodBody = methodBody;
            methodBody.CreateLocalVariable(theEvent.EventType);
            methodBody.CreateLocalVariable(theEvent.EventType);
            methodBody.CreateLocalVariable(theEvent.EventType);
            methodBody.CreateLocalVariable(module.Cache.GetIntrinsic(typeof(bool)));

            var writer = new InstructionWriter();
            //Initialize
            writer.AttachInstructionSequence(initSequence);

            //PropertyChangedHandler handler = this.PropertyChanged
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);           //Get the this pointer
            writer.EmitInstructionField(OpCodeNumber.Ldfld, field); //Get the event's field
            writer.EmitInstruction(OpCodeNumber.Stloc_0);           //store into the first local variable
            writer.DetachInstructionSequence();

            //Loop
            //do {
            writer.AttachInstructionSequence(loopSequence);

            //PropertyChangedHandler handler2 = handler;
            writer.EmitInstruction(OpCodeNumber.Ldloc_0);
            writer.EmitInstruction(OpCodeNumber.Stloc_1);

            //PropertyChangedHandler handler3 = System.Delegate.Remove(handler2, value)
            writer.EmitInstruction(OpCodeNumber.Ldloc_1);
            writer.EmitInstruction(OpCodeNumber.Ldarg_1);
            writer.EmitInstructionMethod(OpCodeNumber.Call, pEWeavingHelper.DelegateRemoveMethod);
            writer.EmitInstructionType(OpCodeNumber.Castclass, theEvent.EventType);
            writer.EmitInstruction(OpCodeNumber.Stloc_2);

            //handler = System.Threading.Interlocked.CompareExchnage<PropertyChangedEventHandler>(ref this.PropertyChtyChanged, handler3, handler2);
            var compareExchangeMethodT = pEWeavingHelper.CompareExchangeMethod;
            var compareExchangeMethod = compareExchangeMethodT.GetGenericInstance(module, theEvent.EventType);
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);           //Load this
            writer.EmitInstructionField(OpCodeNumber.Ldflda, field);
            writer.EmitInstruction(OpCodeNumber.Ldloc_2);
            writer.EmitInstruction(OpCodeNumber.Ldloc_1);
            writer.EmitInstructionMethod(OpCodeNumber.Call, compareExchangeMethod);
            writer.EmitInstruction(OpCodeNumber.Stloc_0);


            //flag = handler != handler2;
            writer.EmitInstruction(OpCodeNumber.Ldloc_0);
            writer.EmitInstruction(OpCodeNumber.Ldloc_1);
            writer.EmitInstruction(OpCodeNumber.Ceq);
            writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
            writer.EmitInstruction(OpCodeNumber.Ceq);
            writer.EmitInstruction(OpCodeNumber.Stloc_3);

            //} while (flag);
            writer.EmitInstruction(OpCodeNumber.Ldloc_3);
            writer.EmitBranchingInstruction(OpCodeNumber.Brtrue_S, loopSequence);

            writer.DetachInstructionSequence();

            //End
            writer.AttachInstructionSequence(endSequence);
            writer.EmitInstruction(OpCodeNumber.Ret);               //Exit the method
            writer.DetachInstructionSequence();

            theEvent.Members.Add(new MethodSemanticDeclaration(MethodSemantics.RemoveOn, method));
        }
        #endregion
    }
}
