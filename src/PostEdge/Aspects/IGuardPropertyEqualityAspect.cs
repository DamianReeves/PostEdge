using System;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;

namespace PostEdge.Aspects {
    public interface IGuardPropertyEqualityAspect: IPostEdgeAspect {
    }

    [MulticastAttributeUsage(MulticastTargets.Property)]
    //[ProvideAspectRole(StandardRoles.DataBinding)]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
    [Serializable]
    public sealed class GuardPropertyEqualityAspect : Aspect, IGuardPropertyEqualityAspect {        
    }

    public static class PostEdgeAspectRoles {
        
    }
}