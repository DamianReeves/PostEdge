using System;

namespace PostEdge.Aspects {
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NoChangeNotificationAttribute : Attribute {
        private PropertyNotificationTypes _excludedChangeNotificationTypes = PropertyNotificationTypes.Both;
        public PropertyNotificationTypes ExcludedChangeNotificationTypes {
            get { return _excludedChangeNotificationTypes; }
            set { _excludedChangeNotificationTypes = value; }
        }
    }
}