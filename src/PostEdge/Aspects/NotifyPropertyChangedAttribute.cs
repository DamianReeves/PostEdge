using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using PostEdge.Aspects.Advices;
using PostEdge.Aspects.Dependencies;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostEdge.Aspects {
    [Serializable]
    [EnhancePropertySetter(CheckEquality = true, InvokePropertyChanged = true)]
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict)]
    [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
    [ProvideAspectRole(StandardRoles.DataBinding)]
    [AspectRoleDependency(AspectDependencyAction.Commute, PostEdgeStandardRoles.NotifyPropertyChanged)]
    public sealed class NotifyPropertyChangedAttribute : TypeLevelAspect, INotifyPropertyChangedAspect {

    }
}
