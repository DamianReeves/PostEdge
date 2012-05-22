using System;
using PostSharp.Aspects.Advices;

namespace PostEdge.Aspects.Advices {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class SetPropertyAdvice: GroupingAdvice {
        private bool _checkEquality = true;
        public bool CheckEquality {
            get { return _checkEquality; }
            set { _checkEquality = value; }
        }
    }
}