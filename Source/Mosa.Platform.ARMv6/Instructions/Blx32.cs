// Copyright (c) MOSA Project. Licensed under the New BSD License.

// This code was generated by an automated template.

using Mosa.Compiler.Framework;

namespace Mosa.Platform.ARMv6.Instructions
{
	/// <summary>
	/// Blx32 - Call a subroutine
	/// </summary>
	/// <seealso cref="Mosa.Platform.ARMv6.ARMv6Instruction" />
	public sealed class Blx32 : ARMv6Instruction
	{
		public override int ID { get { return 704; } }

		internal Blx32()
			: base(1, 3)
		{
		}
	}
}