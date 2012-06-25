using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Extensions {
    internal static class PostSharpExtensions
    {
        #region ModuleDeclaration methods

        public static IMethod GetCtorForGenericType(this ModuleDeclaration moduleDeclaration, Type genericType, params ITypeSignature[] typeList)
        {
            return GetCtorListForGenericType(moduleDeclaration, genericType, typeList).ToList()[0];
        }

        public static IEnumerable<IMethod> GetCtorListForGenericType(this ModuleDeclaration moduleDeclaration, Type genericType, params ITypeSignature[] typeList)
        {
            var actionGeneric = moduleDeclaration.FindType(genericType, BindingOptions.RequireGenericInstance);
            var actionInstance = actionGeneric.GetGenericInstance(moduleDeclaration, typeList);
            return actionInstance.GetSystemType(null, null).GetConstructors().Select(constructor => moduleDeclaration.FindMethod(constructor, BindingOptions.RequireGenericInstance));
        }

        #endregion

        #region IAnnotationValue methods

        public static T ConstructCustomAttribute<T>(this IAnnotationValue source)
            where T : Attribute
        {
            return (T)source.ConstructRuntimeObject();
        }

        public static T GetConstructorArgument<T>(this IAnnotationValue source, int index)
        {
            return (T)source.ConstructorArguments[index].Value.Value;
        }

        public static T GetPropertyValue<T>(this IAnnotationValue source, string name)
        {
            return source.NamedArguments.GetRuntimeValue<T>(name);
        }

        public static T GetNamedValue<T>(this IAnnotationValue source, string name)
        {
            return (T)source.NamedArguments[name].Value.Value;
        }

        #endregion

        #region IAnnotationInstance methods

        public static T GetTargetElement<T>(this IAnnotationInstance source)
            where T : MetadataDeclaration
        {
            return source.TargetElement as T;
        }

        #endregion

        #region Canonical methods

        public static IMethod GetCanonicalGenericInstance(this MethodDefDeclaration methodDefDeclaration)
        {
            return GenericHelper.GetCanonicalGenericInstance(methodDefDeclaration);
        }

        public static IField GetCanonicalGenericInstance(this FieldDefDeclaration fieldDefDeclaration)
        {
            return GenericHelper.GetCanonicalGenericInstance(fieldDefDeclaration);
        }

        public static IType GetCanonicalGenericInstance(this TypeDefDeclaration typeDefDeclaration)
        {
            return GenericHelper.GetCanonicalGenericInstance(typeDefDeclaration);
        }

        #endregion

        #region IMethod methods

        public static IMethod GetGenericInstance(this IMethod source, ModuleDeclaration module, params ITypeSignature[] typeList)
        {
            var genericMethod = GenericHelper.GetCanonicalGenericInstance(source.GetMethodDefinition());
            var genericInstance = genericMethod.GetMethodDefinition().FindGenericInstance(new List<ITypeSignature>(typeList), BindingOptions.Default);
            return (IMethod)genericInstance.Translate(module);
        }

        public static IMethod GetGenericInstanceAlt(this IMethod source, ModuleDeclaration module, params ITypeSignature[] typeList)
        {
            var genericMethod = GenericHelper.GetCanonicalGenericInstance(source.GetMethodDefinition());
            var genericInstance = genericMethod.GetMethodDefinition().FindGenericInstance(new List<ITypeSignature>(typeList), BindingOptions.RequireGenericMask);
            return genericInstance;
        }

        public static IMethod EnsureTypeQualifiedMethod(this IMethod source, ModuleDeclaration module)
        {
            var methodBase = (MethodInfo)source.GetSystemMethod(null, null, BindingOptions.Default);
            return module.FindMethod(methodBase, BindingOptions.RequireGenericInstance);
        }

        public static bool IsStatic(this IMethod source)
        {
            return (source.Attributes & MethodAttributes.Static) != 0;
        }

        #endregion

        #region MethodBody methods
        public static bool ContainsCallToMethod(this MethodBodyDeclaration methodBody, IMethod method) {
            if (methodBody == null) throw new ArgumentNullException("methodBody");
            bool containsCall = false;
            methodBody.ForEachInstruction(reader=> {
                var opCode = reader.CurrentInstruction.OpCodeNumber;
                if(opCode == OpCodeNumber.Call || opCode == OpCodeNumber.Callvirt) {
                    if(reader.CurrentInstruction.MethodOperand != null) {
                        if(reader.CurrentInstruction.MethodOperand.Equals(method)) {
                            containsCall = true;
                            return false;
                        }
                    }
                }
                return true;
            });
            return containsCall;
        }

        #endregion

        #region ITypeSignature methods

        public static ITypeSignature GetGenericInstance(this ITypeSignature source, ModuleDeclaration module, params ITypeSignature[] typeList)
        {
            var genericType = GenericHelper.GetCanonicalGenericInstance(source.GetTypeDefinition()).TranslateType(module);
            var genericMap = new GenericMap(new List<ITypeSignature>(typeList), null);
            return genericType.MapGenericArguments(genericMap);
        }

        public static bool IsStruct(this ITypeSignature type)
        {
            return type.BelongsToClassification(TypeClassifications.Struct);
        }

        public static bool IsPrimitive(this ITypeSignature type)
        {
            var isIntrinisc = type.BelongsToClassification(TypeClassifications.Intrinsic);
            var isEnum = type.BelongsToClassification(TypeClassifications.Enum);
            var isReference = type.BelongsToClassification(TypeClassifications.ReferenceType);
            return !isReference && (isIntrinisc || isEnum);
        }

        public static string GetFullName(this ITypeSignature source)
        {
            return !source.IsGenericInstance
                                 ? source.GetReflectionWrapper(null, null).FullName
                                 : source.GetSystemType(null, null).FullName;
        }

        public static string GetAssemblyQualifiedName(this ITypeSignature source)
        {
            return !source.IsGenericInstance
                                 ? source.GetReflectionWrapper(null, null).AssemblyQualifiedName
                                 : source.GetSystemType(null, null).AssemblyQualifiedName;
        }

        #endregion

        #region IModuleScoped methods

        public static ITypeSignature FindType(this IModuleElement source, Type reflectionType)
        {
            return source.Module.FindType(reflectionType, BindingOptions.RequireGenericInstance);
        }

        public static IMethod FindMethod(this IModuleElement source, MethodBase reflectionMethod)
        {
            return source.Module.FindMethod(reflectionMethod, BindingOptions.RequireGenericInstance);
        }

        #endregion

        #region IDictionary methods

        public static void DefineLocalVariable(this IDictionary<string, LocalVariableSymbol> dictionary, string name, ITypeSignature type, InstructionBlock block)
        {
            LocalVariableSymbol value;
            if (dictionary.TryGetValue(name, out value))
                return;

            value = block.DefineLocalVariable(type, name);
            dictionary.Add(name, value);
        }

        #endregion

        #region Type Methods

        public static IMethod GetGenericTypeMethod(this Type target, ITypeSignature typeArg, string methodName)
        {
            if (!target.IsGenericTypeDefinition)
                throw new ArgumentException(string.Format("The type '{0}' is not an open generic.", target.FullName));

            return typeArg.FindMethod(target.MakeGenericType(typeArg.GetSystemType(null, null)).GetMethod(methodName));
        }

        public static PropertyInfo[] GetAllProperties(this Type type)
        {
            if (!type.IsInterface)
                return type.GetProperties();

            var typeList = new List<Type>
                               {
                                   type
                               };
            typeList.AddRange(type.GetInterfaces());
            return typeList.SelectMany(interfaceType => interfaceType.GetProperties()).ToArray();
        }

        public static PropertyInfo FindProperty(this Type type, string name)
        {
            if (!type.IsInterface)
                return type.GetProperty(name);

            var properties = type.GetAllProperties();
            return properties.SingleOrDefault(property => property.Name == name);
        }

        public static IEnumerable<IMethod> GetMethodsBySignature(this IType type, params string[] signatures) {
            return GetMethodsBySignature(type, true, signatures);
        }

        public static IEnumerable<IMethod> GetMethodsBySignature(this IType type, bool includeDerived, params string[] signatures) {
            if (type == null) throw new ArgumentNullException("type");
            if (signatures == null) throw new ArgumentNullException("signatures");
            if(signatures.Length < 1) throw new ArgumentException("Please provide at least one item.","signatures");
            Contract.EndContractBlock();
            foreach (var method in type.Methods) {
                var reflectionName = method.GetReflectionName();
                Array.FindIndex(signatures, signature => reflectionName == signature);
            }
            yield break;
        }

        public static bool ImplementsInterface(this IType type, ITypeSignature interfaceSignature) {
            if (type == null) throw new ArgumentNullException("type");
            if (interfaceSignature == null) throw new ArgumentNullException("interfaceSignature");
            Contract.EndContractBlock();
            return 
                type.GetTypeDefinition()
                    .InterfaceImplementations.Any(x => x.ImplementedInterface.Equals(interfaceSignature));
        }

        #endregion

        #region PropertyDeclaration methods

        public static IMethod GetGetter(this PropertyDeclaration propertyDeclaration)
        {
            return propertyDeclaration.Getter.GetCanonicalGenericInstance();
        }

        public static IMethod GetSetter(this PropertyDeclaration propertyDeclaration)
        {
            return propertyDeclaration.Setter.GetCanonicalGenericInstance();
        }

        #endregion

        #region CustomAttributeDeclaration Methods
        public static bool ContainsAttributeType(this IEnumerable<CustomAttributeDeclaration> declarations, CustomAttributeDeclaration attribute)
        {
            var attributeName = attribute.Constructor.DeclaringType.GetReflectionName();
            //This is a very convoluted way to get at the attribute's type since this doesn't seem to be saved in the CustomAttributeDeclarartion itself
            return declarations.Any(x => x.Constructor.DeclaringType.GetReflectionName() == attributeName);
        }
        #endregion

    }
}