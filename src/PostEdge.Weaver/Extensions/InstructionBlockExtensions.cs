using System;
using System.Reflection.Emit;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Extensions {
    internal static class InstructionBlockExtensions {
        public static void RedirectBranchInstructions(this InstructionBlock block, InstructionReader reader, InstructionWriter writer, InstructionSequence branchTargetSequence) {
            RedirectBranchInstructions(block, reader, writer, branchTargetSequence, null);
        }

        public static void RedirectBranchInstructions(this InstructionBlock block, InstructionReader reader, InstructionWriter writer, InstructionSequence branchTargetSequence, Predicate<Instruction> predicate) {
            if (block.HasChildrenBlocks) {
                for (InstructionBlock block1 = block.FirstChildBlock; block1 != null; block1 = block1.NextSiblingBlock) {
                    if (!block1.HasExceptionHandlers)
                        RedirectBranchInstructions(block1, reader, writer, branchTargetSequence, predicate);
                }
            }
            reader.JumpToInstructionBlock(block);
            if (block.HasInstructionSequences) {
                for (InstructionSequence sequence = block.FirstInstructionSequence; sequence != null; sequence = sequence.NextSiblingSequence) {
                    bool commit = false;
                    writer.AttachInstructionSequence(sequence);
                    reader.EnterInstructionSequence(sequence);
                    while (reader.ReadInstruction()) {
                        var opCode = reader.CurrentInstruction.OpCodeNumber;
                        var opCodeInfo = reader.CurrentInstruction.OpCodeInfo;
                        if ((opCodeInfo.FlowControl == FlowControl.Branch
                            || opCodeInfo.FlowControl == FlowControl.Cond_Branch)
                            && (predicate == null || predicate(reader.CurrentInstruction))) {
                            commit = true;
                            writer.EmitBranchingInstruction(opCode, branchTargetSequence);
                        } else {
                            reader.CurrentInstruction.Write(writer);
                        }
                    }
                    reader.LeaveInstructionSequence();
                    writer.DetachInstructionSequence(commit);
                }
            }
            reader.LeaveInstructionBlock();
        }
    }    
}