using System;
using System.Reflection;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;

namespace PostEdge.Weaver.Extensions {
    internal static class InstructionWriterExtensions
    {
        #region New_ Methods

        public static void New_EmptyArray(this InstructionWriter instructionWriter, ITypeSignature type)
        {
            instructionWriter.EmitInstruction(OpCodeNumber.Ldc_I4_0);
            instructionWriter.EmitInstructionType(OpCodeNumber.Newarr, type);
        }

        public static void New_Array_1(this InstructionWriter instructionWriter, ITypeSignature type)
        {
            instructionWriter.EmitInstruction(OpCodeNumber.Ldc_I4_1);
            instructionWriter.EmitInstructionType(OpCodeNumber.Newarr, type);
        }

        public static void New_Array_2(this InstructionWriter instructionWriter, ITypeSignature type)
        {
            instructionWriter.EmitInstruction(OpCodeNumber.Ldc_I4_2);
            instructionWriter.EmitInstructionType(OpCodeNumber.Newarr, type);
        }

        public static void New_Object(this InstructionWriter instructionWriter, IMethod ctor, params Action[] arguments)
        {
            foreach (var action in arguments)
            {
                action();
            }

            instructionWriter.EmitInstructionMethod(OpCodeNumber.Newobj, ctor);
        }

        #endregion

        #region Box_ Methods

        public static void Box_SetterValueIfNeeded(this InstructionWriter instructionWriter, PropertyDeclaration propertyDeclaration)
        {
            Get_FirstParameter(instructionWriter);
            EmitBoxIfNeeded(instructionWriter, propertyDeclaration.PropertyType);
        }

        public static void Box_LocalVariableIfNeeded(this InstructionWriter instructionWriter, LocalVariableSymbol symbol)
        {
            instructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, symbol);
            EmitBoxIfNeeded(instructionWriter, symbol.LocalVariable.Type);
        }

        #endregion

        #region Assign_ Methods

        public static void AssignValue_LocalVariable(this InstructionWriter instructionWriter, LocalVariableSymbol symbol, Action argument)
        {
            argument();
            instructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Stloc_S, symbol);
        }

        public static void AssignValue_ToArrayIndexZero(this InstructionWriter instructionWriter, LocalVariableSymbol array, LocalVariableSymbol value)
        {
            instructionWriter.AssignValue_ToArrayIndexZero(
                array,
                () => instructionWriter.Get_LocalVariable(value));
        }

        public static void AssignValue_ToArrayIndexZero(this InstructionWriter instructionWriter, LocalVariableSymbol array, Action valueGetter)
        {
            instructionWriter.Get_LocalVariable(array);
            instructionWriter.EmitInstruction(OpCodeNumber.Ldc_I4_0);
            valueGetter();
            instructionWriter.EmitInstruction(OpCodeNumber.Stelem_Ref);
        }

        public static void AssignValue_ToArrayIndexOne(this InstructionWriter instructionWriter, LocalVariableSymbol array, LocalVariableSymbol value)
        {
            instructionWriter.Get_LocalVariable(array);
            instructionWriter.EmitInstruction(OpCodeNumber.Ldc_I4_1);
            instructionWriter.Get_LocalVariable(value);
            instructionWriter.EmitInstruction(OpCodeNumber.Stelem_Ref);
        }

        #endregion

        #region Compare_ Methods

        public static void Compare_Primitives(this InstructionWriter instructionWriter)
        {
            instructionWriter.EmitInstruction(OpCodeNumber.Ceq);
        }

        public static void Compare_Primitives(this InstructionWriter instructionWriter, Action arg1, Action arg2)
        {
            arg1();
            arg2();
            Compare_Primitives(instructionWriter);
        }

        public static void Compare_Objects(this InstructionWriter instructionWriter, IMethod objectCompareMethod)
        {
            instructionWriter.EmitInstructionMethod(OpCodeNumber.Call, objectCompareMethod);
        }

        #endregion

        #region Get_ Methods

        public static void Get_PropertyValue(this InstructionWriter instructionWriter, PropertyDeclaration propertyDeclaration)
        {
            Call_MethodOnTarget(instructionWriter, propertyDeclaration.GetGetter(), () => instructionWriter.Get_This());
        }

        public static void Get_Null(this InstructionWriter instructionWriter)
        {
            instructionWriter.EmitInstruction(OpCodeNumber.Ldnull);
        }

        public static void Get_This(this InstructionWriter instructionWriter)
        {
            instructionWriter.EmitInstruction(OpCodeNumber.Ldarg_0);
        }

        public static void Get_ConstantZero(this InstructionWriter instructionWriter)
        {
            instructionWriter.EmitInstruction(OpCodeNumber.Ldc_I4_0);
        }

        public static void Get_String(this InstructionWriter instructionWriter, string value)
        {
            instructionWriter.EmitInstructionString(OpCodeNumber.Ldstr, value);
        }

        public static void Get_LocalVariable(this InstructionWriter instructionWriter, LocalVariableSymbol symbol)
        {
            instructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, symbol);
        }

        public static void Get_FirstParameter(this InstructionWriter instructionWriter)
        {
            instructionWriter.EmitInstruction(OpCodeNumber.Ldarg_1);
        }

        public static void Get_RuntimeType(this InstructionWriter instructionWriter, ITypeSignature type, WeavingHelper weavingHelper)
        {
            weavingHelper.GetRuntimeType(type, instructionWriter);
        }

        public static void Get_RuntimeMethod(this InstructionWriter instructionWriter, IMethod method, WeavingHelper weavingHelper)
        {
            var module = method.Module;
            if (!method.IsGenericDefinition && !method.DeclaringType.IsGenericDefinition)
            {
                instructionWriter.EmitInstructionMethod(OpCodeNumber.Ldtoken, method);
                instructionWriter.EmitInstructionMethod(OpCodeNumber.Call, module.Cache.GetItem(module.Cache.MethodBaseGetMethodFromHandle));
            }
            else
            {
                instructionWriter.EmitInstructionMethod(OpCodeNumber.Ldtoken, method.EnsureTypeQualifiedMethod(module));
                instructionWriter.EmitInstructionType(OpCodeNumber.Ldtoken, method.DeclaringType.GetTypeDefinition().GetCanonicalGenericInstance());
                instructionWriter.EmitInstructionMethod(OpCodeNumber.Call, module.Cache.GetItem(module.Cache.MethodBaseGetMethodFromHandle2));
            }
        }

        public static void Get_AddressOf(this InstructionWriter instructionWriter, LocalVariableSymbol localVariable)
        {
            instructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Ldloca, localVariable);
        }

        public static void Get_FtnPtr(this InstructionWriter instructionWriter, IMethod method)
        {
            instructionWriter.EmitInstructionMethod(OpCodeNumber.Ldftn, method);
        }

        public static void Get_MethodInfo(this InstructionWriter writer, IMethod method, WeavingHelper weavingHelper)
        {
            writer.Get_RuntimeMethod(method, weavingHelper);
            writer.Cast_ToType(method.Module.FindType(typeof(MethodInfo)));
        }

        #endregion

        #region Set_Methods

        public static void Set_PropertyValue(this InstructionWriter instructionWriter, PropertyDeclaration propertyDeclaration, Action valueGetter)
        {
            instructionWriter.Get_This();
            valueGetter();
            instructionWriter.EmitInstructionMethod(OpCodeNumber.Call, propertyDeclaration.GetSetter());
        }

        #endregion

        #region Leave_, Cast, Call_ Methods

        public static void Leave_IfTrue(this InstructionWriter instructionWriter, InstructionSequence leaveTarget)
        {
            instructionWriter.EmitBranchingInstruction(OpCodeNumber.Brtrue, leaveTarget);
        }

        public static void Leave_IfFalse(this InstructionWriter instructionWriter, InstructionSequence leaveTarget)
        {
            instructionWriter.EmitBranchingInstruction(OpCodeNumber.Brfalse, leaveTarget);
        }

        public static void Cast_ToType(this InstructionWriter instructionWriter, ITypeSignature type)
        {
            instructionWriter.EmitInstructionType(OpCodeNumber.Castclass, type);
        }

        public static void Call_Method(this InstructionWriter instructionWriter, IMethod method, params Action[] argumentGetterList)
        {
            instructionWriter.Call_MethodOnTarget(
                method,
                () => instructionWriter.Get_This(),
                argumentGetterList);
        }

        public static void Call_MethodOnTarget(this InstructionWriter instructionWriter, IMethod method, Action targetGetter, params Action[] argumentGetterList)
        {
            var isStatic = (method.Attributes & MethodAttributes.Static) == MethodAttributes.Static;
            var isVirtual = (method.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual;

            if (!isStatic)
            {
                targetGetter();
            }

            foreach (var action in argumentGetterList)
            {
                action();
            }

            instructionWriter.EmitInstructionMethod(
                !isVirtual
                    ? OpCodeNumber.Call
                    : OpCodeNumber.Callvirt,
                method);
        }

        #endregion

        #region Private Methods

        private static void EmitBoxIfNeeded(InstructionWriter instructionWriter, ITypeSignature type)
        {
            var isStruct = type.IsStruct();
            if (!isStruct)
                return;

            instructionWriter.EmitInstructionType(OpCodeNumber.Box, type);
        }

        #endregion
    }
}