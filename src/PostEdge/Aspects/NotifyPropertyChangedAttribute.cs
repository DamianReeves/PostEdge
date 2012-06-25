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
        private PropertyNotificationTypes _changeNotificationTypes = PropertyNotificationTypes.Both;
        public PropertyNotificationTypes ChangeNotificationTypes {
            get { return _changeNotificationTypes; }
            set { _changeNotificationTypes = value; }
        }
    }

    [Flags]
    public enum PropertyNotificationTypes {
        None = 0x00,
        PropertyChanged = 0x01,
        PropertyChanging = 0x02,
        Both = PropertyChanged | PropertyChanging
    }
}
