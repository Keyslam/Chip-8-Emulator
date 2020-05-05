using Chip_8_Emulator.Source;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Chip_8_Emulator_Tests
{
	[TestClass]
	public class UnitTests
	{
		[TestMethod]
		public void OP_1NNN()
		{
			Chip8 chip8 = new Chip8(new ushort[]
			{
				0x0210
			});

			chip8.Step();

			Assert.AreEqual(0x0210, chip8.programCounter);
		}
	}
}
