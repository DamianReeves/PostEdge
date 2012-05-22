using System;

namespace PostEdge.Aspects {
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NoChangeNotificationAttribute : Attribute {
    }
}