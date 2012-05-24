using System;
using PostSharp.Aspects.Advices;

namespace PostEdge.Aspects.Advices {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    [Serializable]
    public sealed class EnhancePropertySetterAttribute: Advice {
        private bool _checkEquality = true;
        private bool _invokePropertyChanged = true;

        private string _propertyChangedMethodNames = "OnPropertyChanged;PropertyHasChanged;RaisePropertyChanged";
        private Type _propertyChangedMethodSignature = typeof(Action<string>);

        public bool CheckEquality {
            get { return _checkEquality; }
            set { _checkEquality = value; }
        }

        public bool InvokePropertyChanging { get; set; }

        public bool InvokePropertyChanged {
            get { return _invokePropertyChanged; }
            set { _invokePropertyChanged = value; }
        }

        public string PropertyChangingMethodNames { get; set; }

        public string PropertyChangedMethodNames {
            get { return _propertyChangedMethodNames; }
            set { _propertyChangedMethodNames = value; }
        }

        public Type PropertyChangedMethodSignature {
            get { return _propertyChangedMethodSignature; }
            set { _propertyChangedMethodSignature = value; }
        }
    }
}