﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Framework.IR;

namespace Mosa.Compiler.Framework.Intrinsics
{
	/// <summary>
	/// GetObjectFromAddress
	/// </summary>
	/// <seealso cref="Mosa.Compiler.Framework.IIntrinsicInternalMethod" />
	[ReplacementTarget("Mosa.Runtime.Intrinsic::GetObjectFromAddress")]
	public sealed class GetObjectFromAddress : IIntrinsicInternalMethod
	{
		void IIntrinsicMethod.ReplaceIntrinsicCall(Context context, MethodCompiler methodCompiler)
		{
			var move = methodCompiler.Architecture.Is32BitPlatform ? (BaseInstruction)IRInstruction.MoveInt32 : IRInstruction.MoveInt64;

			context.SetInstruction(move, context.Result, context.Operand1);
		}
	}
}
