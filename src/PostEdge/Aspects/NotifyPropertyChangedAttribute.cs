using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostEdge.Aspects {
    [Serializable]
    [IntroduceInterface(typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict)]
    [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
    [ProvideAspectRole(StandardRoles.DataBinding)]
    public sealed class NotifyPropertyChangedAttribute : InstanceLevelAspect, INotifyPropertyChanged {

        [ImportMember("OnPropertyChanged", IsRequired = false, Order = ImportMemberOrder.AfterIntroductions)]
        public Action<string> OnPropertyChangedMethod;

        [IntroduceMember(Visibility = Visibility.Family, IsVirtual = true, OverrideAction = MemberOverrideAction.Ignore)]
        public void OnPropertyChanged(string propertyName) {
            var handler = this.PropertyChanged;
            if (handler != null) {
                handler(this.Instance, new PropertyChangedEventArgs(propertyName));
            }
        }

        [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore)]
        public event PropertyChangedEventHandler PropertyChanged;

        private IEnumerable<PropertyInfo> SelectProperties(Type type) {
            return from property in type.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public)
                   where !property.IsDefined(typeof(NoChangeNotificationAttribute), false) &&
                         property.CanWrite
                   select property;
        }

        [OnLocationSetValueAdvice, MethodPointcut("SelectProperties")]
        public void OnPropertySet(LocationInterceptionArgs args) {
            // Don't go further if the new value is equal to the old one.
            // (Possibly use object.Equals here).
            if (args.Value == args.GetCurrentValue()) return;

            // Actually sets the value.
            args.ProceedSetValue();

            this.OnPropertyChangedMethod.Invoke(args.Location.Name);

        }
    }
}
