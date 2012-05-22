using System;
using PostSharp.Aspects.Advices;

namespace PostEdge.Aspects.Advices {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class EnhancePropertySetterAttribute: Advice {
        private bool _checkEquality = true;

        public bool CheckEquality {
            get { return _checkEquality; }
            set { _checkEquality = value; }
        }
    }
}