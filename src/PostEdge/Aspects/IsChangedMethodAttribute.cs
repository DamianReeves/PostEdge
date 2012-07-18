namespace PostEdge.Aspects {
    using System;
    using PostSharp.Extensibility;
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module |
                    AttributeTargets.Class | AttributeTargets.Struct |
                    AttributeTargets.Interface| AttributeTargets.Method,
        AllowMultiple = true, Inherited = true)]
    [MulticastAttributeUsage(MulticastTargets.Method, AllowMultiple = false)]
    public class IsChangedMethodAttribute: MulticastAttribute {        
    }
}