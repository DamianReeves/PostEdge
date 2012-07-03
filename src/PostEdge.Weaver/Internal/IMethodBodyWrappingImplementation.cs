using System;
using System.ComponentModel.Composition;
using System.Linq;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Internal {
    public interface IMethodBodyWrappingImplementation :
        IMethodBodyOnEntryImplementation, IMethodBodyOnSuccessImplementation
        , IMethodBodyOnExitImplementation, IMethodBodyOnExceptionImplementation {
    }

    public interface IMethodBodyWrappingImplementationInstance {
        void Initialize(MethodBodyTransformationContext context);
    }

    public interface IMethodBodyOnEntryImplementation : IMethodBodyWrappingImplementationInstance {
        void ImplementOnEntry(InstructionBlock block, InstructionWriter writer);
    }

    public interface IMethodBodyOnEntryImplementation<in TContext> : IMethodBodyWrappingImplementationInstance {
        void ImplementOnEntry(InstructionBlock block, InstructionWriter writer, TContext context);
    }

    public interface IMethodBodyOnSuccessImplementation : IMethodBodyWrappingImplementationInstance {
        void ImplementOnSuccess(InstructionBlock block, InstructionWriter writer);
    }

    public interface IMethodBodyOnExitImplementation : IMethodBodyWrappingImplementationInstance {
        void ImplementOnExit(InstructionBlock block, InstructionWriter writer);
    }

    public interface IMethodBodyOnExceptionImplementation : IMethodBodyWrappingImplementationInstance {
        void ImplementOnException(InstructionBlock block, ITypeSignature exceptionType, InstructionWriter writer);
    }

    public interface IMethodBodyWrappingImplementationMetadata {
        string[] Transformations { get; }
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

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class OnEntryMethodBodyWrappingImplementationAttribute : ExportAttribute {
        public OnEntryMethodBodyWrappingImplementationAttribute(string transformation, params string[] otherTransformations)
            : base(typeof(IMethodBodyOnEntryImplementation)) {
            if (transformation == null) throw new ArgumentNullException("transformation");
            if (String.IsNullOrEmpty(transformation)) throw new ArgumentException("A transformation name is required.", "transformation");
            if (otherTransformations == null || otherTransformations.Length == 0) {
                Transformations = new[] { transformation };
            } else {
                Transformations =
                    new[] { transformation }.Concat(otherTransformations).ToArray();
            }
        }

        public string[] Transformations { get; set; }
        public int Priority { get; set; }
    }
}