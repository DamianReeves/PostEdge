using System;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Internal {
    internal interface IMethodBodyWrappingStrategy :
        IMethodBodyOnEntryStrategy, IMethodBodyOnSuccessStrategy
        , IMethodBodyOnExitStrategy, IMethodBodyOnExceptionStrategy {        
    }

    internal interface IMethodBodyOnEntryStrategy {
        void ImplementOnEntry(InstructionBlock block, InstructionWriter writer);
    }

    internal interface IMethodBodyOnSuccessStrategy {
        void ImplementOnSuccess(InstructionBlock block, InstructionWriter writer);
    }

    internal interface IMethodBodyOnExitStrategy {
        void ImplementOnExit(InstructionBlock block, InstructionWriter writer);
    }

    internal interface IMethodBodyOnExceptionStrategy {
        void ImplementOnException(InstructionBlock block, ITypeSignature exceptionType, InstructionWriter writer);
    }

    internal interface IMethodBodyWrappingStrategyMetadata {
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