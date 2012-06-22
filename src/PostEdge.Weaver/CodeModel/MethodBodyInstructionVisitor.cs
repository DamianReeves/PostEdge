using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.CodeModel {
    public abstract class MethodBodyInstructionVisitorBase: IMethodBodyVisitor {
        public void EnterInstructionBlock(InstructionBlock instructionBlock, InstructionBlockExceptionHandlingKind exceptionHandlingKind) {}
        public void LeaveInstructionBlock(InstructionBlock instructionBlock, InstructionBlockExceptionHandlingKind exceptionHandlingKind) {}
        public void EnterInstructionSequence(InstructionSequence instructionSequence) {}
        public void LeaveInstructionSequence(InstructionSequence instructionSequence) {}
        public void EnterExceptionHandler(ExceptionHandler exceptionHandler) {}
        public void LeaveExceptionHandler(ExceptionHandler exceptionHandler) {}

        public abstract void VisitInstruction(InstructionReader instructionReader);
    }
}