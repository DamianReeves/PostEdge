using System;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.CodeModel {
    public class InstructionContext {
        public InstructionContext() {}

        public InstructionContext(InstructionReader reader) {
            if (reader == null) throw new ArgumentNullException("reader");
            Instruction = reader.CurrentInstruction;
            InstructionBlock = reader.CurrentInstructionBlock;
            InstructionSequence = reader.CurrentInstructionSequence;
        }

        public Instruction Instruction { get; set; }
        public InstructionSequence InstructionSequence { get; set; }
        public InstructionBlock InstructionBlock { get; set; }
    }
}