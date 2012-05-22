using System;

namespace PostEdge.Aspects {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SafeWhenDisposedAttribute : Attribute {
    }
}