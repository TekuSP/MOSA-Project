// Copyright (c) MOSA Project. Licensed under the New BSD License.

// This code was generated by an automated template.

using Mosa.Compiler.Framework;

namespace Mosa.Platform.x64.Instructions
{
	/// <summary>
	/// Rcr32
	/// </summary>
	/// <seealso cref="Mosa.Platform.x64.X64Instruction" />
	public sealed class Rcr32 : X64Instruction
	{
		public override int ID { get { return 551; } }

		internal Rcr32()
			: base(1, 2)
		{
		}

		public override bool IsZeroFlagModified { get { return true; } }

		public override bool IsCarryFlagModified { get { return true; } }

		public override bool IsOverflowFlagModified { get { return true; } }
	}
}