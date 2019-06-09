// Copyright (c) MOSA Project. Licensed under the New BSD License.

// This code was generated by an automated template.

using Mosa.Compiler.Framework;

namespace Mosa.Platform.x86.Instructions
{
	/// <summary>
	/// Cvtss2sd
	/// </summary>
	/// <seealso cref="Mosa.Platform.x86.X86Instruction" />
	public sealed class Cvtss2sd : X86Instruction
	{
		public override int ID { get { return 215; } }

		internal Cvtss2sd()
			: base(1, 1)
		{
		}

		public override void Emit(InstructionNode node, BaseCodeEmitter emitter)
		{
			System.Diagnostics.Debug.Assert(node.ResultCount == 1);
			System.Diagnostics.Debug.Assert(node.OperandCount == 1);

			emitter.OpcodeEncoder.AppendByte(0xF3);
			emitter.OpcodeEncoder.AppendByte(0x0F);
			emitter.OpcodeEncoder.AppendByte(0x5A);
			emitter.OpcodeEncoder.Append2Bits(0b11);
			emitter.OpcodeEncoder.Append3Bits(node.Result.Register.RegisterCode);
			emitter.OpcodeEncoder.Append3Bits(node.Operand1.Register.RegisterCode);
		}
	}
}
