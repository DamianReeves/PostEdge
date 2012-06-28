using System;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Internal {
    internal class MethodBodyWrappingImplementationContext {
        private readonly ITypeSignature[] _exceptionTypes;
        private readonly string _transformation;
        private readonly MethodBodyWrappingImplementationType _wrappingImplementationType;

        public MethodBodyWrappingImplementationContext(string transformation, MethodBodyWrappingImplementationType wrappingImplementationType) {
            if (transformation == null) throw new ArgumentNullException("transformation");
            _transformation = transformation;
            _wrappingImplementationType = wrappingImplementationType;
        }

        public MethodBodyWrappingImplementationContext(string transformation, MethodBodyWrappingImplementationType wrappingImplementationType, ITypeSignature[] exceptionTypes) {
            if (transformation == null) throw new ArgumentNullException("transformation");
            _transformation = transformation;
            _exceptionTypes = exceptionTypes;
        }

        public string Transformation {
            get { return _transformation; }
        }

        public MethodBodyWrappingImplementationType WrappingImplementationType {
            get { return _wrappingImplementationType; }
        }

        public bool CallOriginal {
            get { return _wrappingImplementationType.HasFlag(MethodBodyWrappingImplementationType.CallOriginal); }
        }

        public bool ImplementOnEntry {
            get { return _wrappingImplementationType.HasFlag(MethodBodyWrappingImplementationType.OnEntry); }
        }

        public bool ImplementOnSuccess {
            get { return _wrappingImplementationType.HasFlag(MethodBodyWrappingImplementationType.OnSuccess); }
        }

        public bool ImplementOnException {
            get { return _wrappingImplementationType.HasFlag(MethodBodyWrappingImplementationType.OnException); }
        }

        public bool ImplementOnExit {
            get { return _wrappingImplementationType.HasFlag(MethodBodyWrappingImplementationType.OnExit); }
        }

        public ITypeSignature[] ExceptionTypes {
            get { return _exceptionTypes; }
        }
    }
}