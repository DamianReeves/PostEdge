using System;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Internal {
    internal interface IMethodBodyWrappingStrategy :
        IMethodBodyOnEntryImplementation, IMethodBodyOnSuccessImplementation
        , IMethodBodyOnExitImplementation, IMethodBodyOnExceptionImplementation {        
    }

    internal interface IMethodBodyOnEntryImplementation {
        void ImplementOnEntry(InstructionBlock block, InstructionWriter writer);
    }

    internal interface IMethodBodyOnSuccessImplementation {
        void ImplementOnSuccess(InstructionBlock block, InstructionWriter writer);
    }

    internal interface IMethodBodyOnExitImplementation {
        void ImplementOnExit(InstructionBlock block, InstructionWriter writer);
    }

    internal interface IMethodBodyOnExceptionImplementation {
        void ImplementOnException(InstructionBlock block, ITypeSignature exceptionType, InstructionWriter writer);
    }

    internal interface IMethodBodyWrappingImplementationMetadata {
        string Transformation { get; }
        //MethodBodyWrappingImplementationType ImplementationType { get; }
        int Priority { get; }
    }

    [Flags]
    public enum MethodBodyWrappingImplementationType {
        None = 0x0000,
        CallOriginal = 0x0001,
        OnEntry = 0x0002,
        OnSuccess = 0x0004,
        OnExit = 0x0008,
        OnException = 0x0010
    }
}