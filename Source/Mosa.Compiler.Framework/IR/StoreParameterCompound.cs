// Copyright (c) MOSA Project. Licensed under the New BSD License.

// This code was generated by an automated template.

namespace Mosa.Compiler.Framework.IR
{
	/// <summary>
	/// StoreParameterCompound
	/// </summary>
	/// <seealso cref="Mosa.Compiler.Framework.IR.BaseIRInstruction" />
	public sealed class StoreParameterCompound : BaseIRInstruction
	{
		public StoreParameterCompound()
			: base(2, 0)
		{
		}

		public override bool IsMemoryWrite { get { return true; } }
	}
}

