// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Framework;
using Mosa.Platform.Intel;
using System.Diagnostics;

namespace Mosa.Platform.x86.Stages
{
	/// <summary>
	/// Final Tweak Transformation Stage
	/// </summary>
	/// <seealso cref="Mosa.Platform.x86.BaseTransformationStage" />
	public sealed class FinalTweakStage : BaseTransformationStage
	{
		protected override void PopulateVisitationDictionary()
		{
			AddVisitation(X86.Call, CallReg);
			AddVisitation(X86.Mov32, Mov32);
			AddVisitation(X86.MovLoad16, MovLoad16);
			AddVisitation(X86.MovLoad8, MovLoad8);
			AddVisitation(X86.Movsd, Movsd);
			AddVisitation(X86.Movss, Movss);
			AddVisitation(X86.MovStore16, MovStore16);
			AddVisitation(X86.MovStore8, MovStore8);
			AddVisitation(X86.Movzx16To32, Movzx16To32);
			AddVisitation(X86.Movzx8To32, Movzx8To32);
			AddVisitation(X86.Nop, Nop);
			AddVisitation(X86.SetByteIfCarry, Setcc);
			AddVisitation(X86.SetByteIfEqual, Setcc);
			AddVisitation(X86.SetByteIfGreaterOrEqual, Setcc);
			AddVisitation(X86.SetByteIfGreaterThan, Setcc);
			AddVisitation(X86.SetByteIfLessOrEqual, Setcc);
			AddVisitation(X86.SetByteIfLessThan, Setcc);
			AddVisitation(X86.SetByteIfNoCarry, Setcc);
			AddVisitation(X86.SetByteIfNoOverflow, Setcc);
			AddVisitation(X86.SetByteIfNoParity, Setcc);
			AddVisitation(X86.SetByteIfNotEqual, Setcc);
			AddVisitation(X86.SetByteIfNotSigned, Setcc);
			AddVisitation(X86.SetByteIfNotZero, Setcc);
			AddVisitation(X86.SetByteIfOverflow, Setcc);
			AddVisitation(X86.SetByteIfParity, Setcc);
			AddVisitation(X86.SetByteIfSigned, Setcc);
			AddVisitation(X86.SetByteIfUnsignedGreaterOrEqual, Setcc);
			AddVisitation(X86.SetByteIfUnsignedGreaterThan, Setcc);
			AddVisitation(X86.SetByteIfUnsignedLessOrEqual, Setcc);
			AddVisitation(X86.SetByteIfUnsignedLessThan, Setcc);
			AddVisitation(X86.SetByteIfZero, Setcc);
		}

		#region Visitation Methods

		public void CallReg(Context context)
		{
			Debug.Assert(context.Operand1 != null);
			Debug.Assert(context.Operand1.IsCPURegister);

			var before = context.Previous;

			while (before.IsEmpty && !before.IsBlockStartInstruction)
			{
				before = before.Previous;
			}

			if (before == null || before.IsBlockStartInstruction)
				return;

			if (before.Instruction != X86.Mov32)
				return;

			if (!before.Result.IsCPURegister)
				return;

			if (context.Operand1.Register != before.Result.Register)
				return;

			before.SetInstruction(X86.Call, null, before.Operand1);
			context.Empty();
		}

		public void Mov32(Context context)
		{
			var source = context.Operand1;
			var result = context.Result;

			Debug.Assert(result.IsCPURegister);

			if (source.IsCPURegister && result.Register == source.Register)
			{
				context.Empty();
			}
		}

		public void MovLoad16(Context context)
		{
			var result = context.Result;

			Debug.Assert(result.IsCPURegister);

			if (result.Register == GeneralPurposeRegister.ESI || result.Register == GeneralPurposeRegister.EDI)
			{
				var source = context.Operand1;
				var offset = context.Operand2;

				context.SetInstruction(X86.MovLoad32, result, source, offset);
				context.AppendInstruction(X86.And32, result, result, CreateConstant(0x0000FFFF));
			}
		}

		public void MovLoad8(Context context)
		{
			var result = context.Result;

			Debug.Assert(result.IsCPURegister);

			if (result.Register == GeneralPurposeRegister.ESI || result.Register == GeneralPurposeRegister.EDI)
			{
				var source = context.Operand1;
				var offset = context.Operand2;

				context.SetInstruction(X86.MovLoad32, result, source, offset);
				context.AppendInstruction(X86.And32, result, result, CreateConstant(0x000000FF));
			}
		}

		public void Movsd(Context context)
		{
			Debug.Assert(context.Result.IsCPURegister);
			Debug.Assert(context.Operand1.IsCPURegister);

			if (context.Result.Register == context.Operand1.Register)
			{
				context.Empty();
			}
		}

		public void Movss(Context context)
		{
			Debug.Assert(context.Result.IsCPURegister);
			Debug.Assert(context.Operand1.IsCPURegister);

			if (context.Result.Register == context.Operand1.Register)
			{
				context.Empty();
			}
		}

		public void MovStore16(Context context)
		{
			var value = context.Operand3;

			if (value.IsCPURegister && (value.Register == GeneralPurposeRegister.ESI || value.Register == GeneralPurposeRegister.EDI))
			{
				var dest = context.Operand1;
				var offset = context.Operand2;

				Operand temporaryRegister = null;

				if (dest.Register != GeneralPurposeRegister.EAX && offset.Register != GeneralPurposeRegister.EAX)
				{
					temporaryRegister = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EAX);
				}
				else if (dest.Register != GeneralPurposeRegister.EBX && offset.Register != GeneralPurposeRegister.EBX)
				{
					temporaryRegister = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EBX);
				}
				else if (dest.Register != GeneralPurposeRegister.ECX && offset.Register != GeneralPurposeRegister.ECX)
				{
					temporaryRegister = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.ECX);
				}
				else
				{
					temporaryRegister = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EDX);
				}

				context.SetInstruction2(X86.XChg32, temporaryRegister, value, value, temporaryRegister);
				context.AppendInstruction(X86.MovStore16, null, dest, offset, temporaryRegister);
				context.AppendInstruction2(X86.XChg32, value, temporaryRegister, temporaryRegister, value);
			}
		}

		public void MovStore8(Context context)
		{
			var value = context.Operand3;

			if (value.IsCPURegister && (value.Register == GeneralPurposeRegister.ESI || value.Register == GeneralPurposeRegister.EDI))
			{
				var dest = context.Operand1;
				var offset = context.Operand2;

				Operand temporaryRegister = null;

				if (dest.Register != GeneralPurposeRegister.EAX && offset.Register != GeneralPurposeRegister.EAX)
				{
					temporaryRegister = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EAX);
				}
				else if (dest.Register != GeneralPurposeRegister.EBX && offset.Register != GeneralPurposeRegister.EBX)
				{
					temporaryRegister = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EBX);
				}
				else if (dest.Register != GeneralPurposeRegister.ECX && offset.Register != GeneralPurposeRegister.ECX)
				{
					temporaryRegister = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.ECX);
				}
				else
				{
					temporaryRegister = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EDX);
				}

				context.SetInstruction2(X86.XChg32, temporaryRegister, value, value, temporaryRegister);
				context.AppendInstruction(X86.MovStore8, null, dest, offset, temporaryRegister);
				context.AppendInstruction2(X86.XChg32, value, temporaryRegister, temporaryRegister, value);
			}
		}

		public void Movzx16To32(Context context)
		{
			Debug.Assert(context.Result.IsCPURegister);

			// Movzx8To32 can not use with ESI or EDI registers
			if (context.Operand1.Register != GeneralPurposeRegister.ESI && context.Operand1.Register != GeneralPurposeRegister.EDI)
				return;

			var result = context.Result;
			var source = context.Operand1;

			if (source.Register != result.Register)
			{
				context.SetInstruction(X86.Mov32, result, source);
				context.AppendInstruction(X86.And32, result, result, CreateConstant(0xFFFF));
			}
			else
			{
				context.SetInstruction(X86.And32, result, result, CreateConstant(0xFFFF));
			}
		}

		public void Movzx8To32(Context context)
		{
			Debug.Assert(context.Result.IsCPURegister);

			// Movzx8To32 can not use with ESI or EDI registers
			if (context.Operand1.Register != GeneralPurposeRegister.ESI && context.Operand1.Register != GeneralPurposeRegister.EDI)
				return;

			var result = context.Result;
			var source = context.Operand1;

			if (source.Register != result.Register)
			{
				context.SetInstruction(X86.Mov32, result, source);
				context.AppendInstruction(X86.And32, result, result, CreateConstant(0xFF));
			}
			else
			{
				context.SetInstruction(X86.And32, result, result, CreateConstant(0xFF));
			}
		}

		public void Nop(Context context)
		{
			context.Empty();
		}

		public void Setcc(Context context)
		{
			Debug.Assert(context.Result.IsCPURegister);

			var result = context.Result;
			var instruction = context.Instruction;

			Debug.Assert(result.IsCPURegister);

			// SETcc can not use with ESI or EDI registers
			if (result.Register == GeneralPurposeRegister.ESI || result.Register == GeneralPurposeRegister.EDI)
			{
				var condition = context.ConditionCode;

				var eax = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EAX);

				context.SetInstruction2(X86.XChg32, eax, result, result, eax);
				context.AppendInstruction(instruction, condition, eax);
				context.AppendInstruction2(X86.XChg32, result, eax, eax, result);
			}
		}

		#endregion Visitation Methods
	}
}
