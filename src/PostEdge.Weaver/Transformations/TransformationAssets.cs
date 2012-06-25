using PostSharp.Aspects.Internals;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Transformations {
    internal sealed class TransformationAssets {
        public IMethod ObjectEqualsMethod { get; private set; }
        public ITypeSignature LocationBindingTypeSignature { get; private set; }
        public IMethod SetValueMethod { get; private set; }
        public IMethod GetValueMethod { get; private set; }
        public ITypeSignature ObjectTypeSignature { get; private set; }
        public TransformationAssets(ModuleDeclaration module) {
            ObjectTypeSignature = module.FindType(typeof(object));
            ObjectEqualsMethod = module.FindMethod(typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) }), BindingOptions.Default);
            LocationBindingTypeSignature = module.FindType(typeof(LocationBinding<>));
            SetValueMethod = module.FindMethod(LocationBindingTypeSignature, "SetValue", x => x.DeclaringType.IsGenericDefinition);
            GetValueMethod = module.FindMethod(LocationBindingTypeSignature, "GetValue", x => x.DeclaringType.IsGenericDefinition);
        }
    }
}