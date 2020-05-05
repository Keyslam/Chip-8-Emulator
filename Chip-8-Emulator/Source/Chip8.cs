using System;

namespace Chip_8_Emulator.Source
{
	public class Chip8
	{
		private short opcode = default;

		private byte[] memory = new byte[0x1000];
		// 0x000-0x1FF - Chip 8 interpreter (contains font set in emu)
		// 0x050-0x0A0 - Used for the built in 4x5 pixel font set(0-F)
		// 0x200-0xFFF - Program ROM and work RAM

		private byte[] registers = new byte[16];

		private short indexRegister = default;
		private short programCounter = default;

		private short[] stack = new short[16];
		private short stackPointer = default;

		public byte[] gfx = new byte[64 * 32];

		private byte delayTimer = default;
		private byte soundTimer = default;

		private byte[] key = new byte[16];

		private bool refreshScreen = false;

		private Random random = new Random();

		public Chip8(ushort[] instructions)
		{
			Reset();

			LoadFontSet();
			LoadProgram(instructions);
		}

		public Chip8(byte[] instructions)
		{
			Reset();

			LoadFontSet();
			LoadProgram(instructions);
		}

		private void Reset()
		{
			opcode = 0;

			Array.Clear(memory, 0, memory.Length);
			Array.Clear(registers, 0, registers.Length);

			indexRegister = 0;
			programCounter = 0x200;

			Array.Clear(stack, 0, stack.Length);
			stackPointer = 0;

			Array.Clear(gfx, 0, gfx.Length);

			delayTimer = 0;
			soundTimer = 0;

			Array.Clear(key, 0, key.Length);
		}

		public void LoadFontSet()
		{
			byte[] data = new byte[] 
			{
				  0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
				  0x20, 0x60, 0x20, 0x20, 0x70, // 1
				  0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
				  0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
				  0x90, 0x90, 0xF0, 0x10, 0x10, // 4
				  0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
				  0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
				  0xF0, 0x10, 0x20, 0x40, 0x40, // 7
				  0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
				  0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
				  0xF0, 0x90, 0xF0, 0x90, 0x90, // A
				  0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
				  0xF0, 0x80, 0x80, 0x80, 0xF0, // C
				  0xE0, 0x90, 0x90, 0x90, 0xE0, // D
				  0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
				  0xF0, 0x80, 0xF0, 0x80, 0x80  // F
			};

			for (int i = 0; i < data.Length; i++)
				memory[i] = data[i];
		}

		public void LoadProgram(ushort[] instructions)
		{
			for (int i = 0; i < instructions.Length; i++)
			{
				ushort instruction = instructions[i];

				byte upper = Convert.ToByte(instruction >> 8 & 0xFF);
				byte lower = Convert.ToByte(instruction & 0xFF);

				int index = (i * 2) + 512;

				memory[index] = upper;
				memory[index + 1] = lower;
			}
		}

		public void LoadProgram(byte[] instructions)
		{
			for (int i = 0; i < instructions.Length; i++)
				memory[i + 0x200] = instructions[i];
		}

		public void Step()
		{
			bool incrementPC = true;

			FetchOpcode();

			short nnn;
			byte nn;
			byte n;
			byte vx;
			byte vy;

			byte data;
			byte count;
			short address;

			Console.WriteLine("Opcode: " + opcode.ToString("X4"));

			switch (opcode & 0xF000)
			{
				case 0x0000:
					switch (opcode & 0x00FF)
					{
						case 0x00E0: // Clears the screen. 
							Array.Clear(gfx, 0, gfx.Length);
							refreshScreen = true;

							break;
						case 0x00EE: // Returns from a subroutine. 
							stackPointer--;
							programCounter = stack[stackPointer];

							break;
						default:
							UnknownOpcode();
							break;
					}
					break;
				case 0x1000: // (1NNN) Jumps to address NNN. 
					programCounter = (short)(opcode & 0x0FFF);
					incrementPC = false;

					break;
				case 0x2000: // (2NNN) Calls subroutine at NNN. 
					stack[stackPointer] = programCounter;
					stackPointer++;
					programCounter = (short)(opcode & 0x0FFF);
					incrementPC = false;

					break;
				case 0x3000: // (3XNN) Skips the next instruction if VX equals NN. (Usually the next instruction is a jump to skip a code block) 
					vx = registers[(short)(opcode & 0x0F00) >> 8];
					nn = (byte)(opcode & 0x00FF);

					if (vx == nn)
						programCounter += 2;

					break;
				case 0x4000: // (4XNN) Skips the next instruction if VX doesn't equal NN. (Usually the next instruction is a jump to skip a code block) 
					vx = registers[(short)(opcode & 0x0F00) >> 8];
					nn = (byte)(opcode & 0x00FF);

					if (vx != nn)
						programCounter += 2;

					break;
				case 0x5000: // (5XY0) Skips the next instruction if VX equals VY. (Usually the next instruction is a jump to skip a code block) 
					vx = registers[(short)(opcode & 0x0F00) >> 8];
					vy = registers[(short)(opcode & 0x00F0) >> 4];

					if (vx == vy)
						programCounter += 2;

					break;
				case 0x6000: // (6XNN) Sets VX to NN.
					nn = (byte)(opcode & 0x00FF);
					registers[(short)(opcode & 0x0F00) >> 8] = nn;

					break;
				case 0x7000: // (7XNN) Adds NN to VX. (Carry flag is not changed) 
					nn = (byte)(opcode & 0x00FF);
					registers[(short)(opcode & 0x0F00) >> 8] += nn;

					break;
				case 0x8000:
					switch (opcode & 0x000F)
					{
						case 0x0000: // (8XY0) Sets VX to the value of VY. 
							vy = registers[(short)(opcode & 0x00F0) >> 4];

							registers[(short)(opcode & 0x0F00) >> 8] = vy;

							break;
						case 0x0001: // (8XY1) Sets VX to VX or VY. (Bitwise OR operation) 
							vx = registers[(short)(opcode & 0x0F00) >> 8];
							vy = registers[(short)(opcode & 0x00F0) >> 4];

							registers[(short)(opcode & 0x0F00) >> 8] = (byte)(vx | vy);

							break;
						case 0x0002: // (8XY2) Sets VX to VX and VY. (Bitwise AND operation) 
							vx = registers[(short)(opcode & 0x0F00) >> 8];
							vy = registers[(short)(opcode & 0x00F0) >> 4];

							registers[(short)(opcode & 0x0F00) >> 8] = (byte)(vx & vy);

							break;
						case 0x0003: // (8XY3) Sets VX to VX xor VY. 
							vx = registers[(short)(opcode & 0x0F00) >> 8];
							vy = registers[(short)(opcode & 0x00F0) >> 4];

							registers[(short)(opcode & 0x0F00) >> 8] = (byte)(vx ^ vy);

							break;
						case 0x0004: // (8XY4) Adds VY to VX. VF is set to 1 when there's a carry, and to 0 when there isn't. 
							vx = registers[opcode & 0x0F00 >> 8];
							vy = registers[opcode & 0x00F0 >> 4];

							if (vy > 0xFF - vx)
								registers[0xF] = 1;
							else
								registers[0xF] = 0;

							registers[opcode & 0x0F00 >> 8] += vy;

							break;
						case 0x0005: // (8XY5) VY is subtracted from VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
							vx = registers[opcode & 0x0F00 >> 8];
							vy = registers[opcode & 0x00F0 >> 4];

							if (vy > vx)
								registers[0xF] = 1;
							else
								registers[0xF] = 0;

							registers[opcode & 0x0F00 >> 8] -= vy;

							break;
						case 0x0006: // (8XY6) Stores the least significant bit of VX in VF and then shifts VX to the right by 1.
							vx = registers[opcode & 0x0F00 >> 8];
							data = (byte)(vx & 0x01);

							registers[0xF] = data;

							registers[opcode & 0x0F00 >> 8] = (byte)(vx >> 1);

							break;
						case 0x0007: // (8XY7) Sets VX to VY minus VX. VF is set to 0 when there's a borrow, and 1 when there isn't. 
							Console.WriteLine("Not implemented: " + opcode.ToString("X4"));

							break;
						case 0x000E: // (8XYE) Stores the most significant bit of VX in VF and then shifts VX to the left by 1.
							vx = registers[opcode & 0x0F00 >> 8];
							data = (byte)(vx & 0x80);

							registers[0xF] = data;

							registers[opcode & 0x0F00 >> 8] = (byte)(vx << 1);

							break;
						default:
							UnknownOpcode();
							break;
					}
					break;
				case 0x9000: // (9XY0) Skips the next instruction if VX doesn't equal VY. (Usually the next instruction is a jump to skip a code block) 
					vx = registers[(byte)(opcode & 0x0F00) >> 8];
					vy = registers[(byte)(opcode & 0x00F0) >> 4];

					if (vx != vy)
						programCounter += 2;

					break;
				case 0xA000: // (ANNN) Sets I to the address NNN. 
					indexRegister = (short)(opcode & 0x0FFF);
					break;
				case 0xB000: // (BNNN) Jumps to the address NNN plus V0.
					nnn = (byte)(opcode & 0x0FFF);

					programCounter = (short)(registers[0] + nnn);

					break;
				case 0xC000: // (CXNN) Sets VX to the result of a bitwise and operation on a random number (Typically: 0 to 255) and NN. 
					nn = (byte)(opcode & 0x00FF);
					registers[(byte)(opcode & 0x0F00)] = (byte)(random.Next(0, 255) & nn);

					break;
				case 0xD000: // (DXYN) Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels. Each row of 8 pixels is read as bit-coded starting from memory location I; I value doesn’t change after the execution of this instruction. As described above, VF is set to 1 if any screen pixels are flipped from set to unset when the sprite is drawn, and to 0 if that doesn’t happen 
					vx = registers[(byte)(opcode & 0x0F00) >> 8];
					vy = registers[(byte)(opcode & 0x00F0) >> 4];
					n = (byte)(opcode & 0x000F);

					registers[0xF] = 0;

					for (int i = 0; i < n; i++)
					{
						data = memory[indexRegister + i];

						for (int j = 0; j < 8; j++)
						{
							if (vx + (vy + i) * 64 + j > 64 * 32)
								continue;

							bool current = gfx[vx + (vy + i) * 64 + j] != 0;
							bool pixel = (data & (0x80 >> j)) != 0;

							gfx[vx + (vy + i) * 64 + j] = (byte)((current ^ pixel) ? 0xFF : 0x00);

							if (current == pixel)
								registers[0xF] = 1;
						}
					}

					break;
				case 0xE000:
					switch (opcode & 0x00FF)
					{
						case 0x009E: // (EX9E) Skips the next instruction if the key stored in VX is pressed. (Usually the next instruction is a jump to skip a code block) 
							Console.WriteLine("Not implemented: " + opcode.ToString("X4"));

							break;
						case 0x00A1: // (EXA1) Skips the next instruction if the key stored in VX isn't pressed. (Usually the next instruction is a jump to skip a code block) 
							Console.WriteLine("Not implemented: " + opcode.ToString("X4"));

							break;
						default:
							UnknownOpcode();
							break;
					}
					break;
				case 0xF000:
					switch (opcode & 0x00FF)
					{
						case 0x0007: // (FX07) Sets VX to the value of the delay timer. 
							Console.WriteLine("Not implemented: " + opcode.ToString("X4"));

							break;
						case 0x000A: // (FX0A) A key press is awaited, and then stored in VX. (Blocking Operation. All instruction halted until next key event) 
							Console.WriteLine("Not implemented: " + opcode.ToString("X4"));

							break;
						case 0x0015: // (FX15) Sets the delay timer to VX.
							Console.WriteLine("Not implemented: " + opcode.ToString("X4"));

							break;
						case 0x0018: // (FX18) Sets the sound timer to VX. 
							Console.WriteLine("Not implemented: " + opcode.ToString("X4"));

							break;
						case 0x001E: // (FX1E) Adds VX to I. VF is set to 1 when there is a range overflow (I+VX>0xFFF), and to 0 when there isn't.
							vx = registers[(opcode & 0x0F00) >> 8];

							if (indexRegister + vx > 0xFFF)
								registers[15] = 1;
							else
								registers[15] = 0;

							indexRegister += vx;

							break;
						case 0x0029: // (FX29) Sets I to the location of the sprite for the character in VX. Characters 0-F (in hexadecimal) are represented by a 4x5 font. 
							byte character = registers[(opcode & 0x0F00) >> 8];
							byte index = (byte)(character * 5);

							indexRegister = index;

							break;
						case 0x0033: // (FX33) Stores the binary-coded decimal representation of VX, with the most significant of three digits at the address in I, the middle digit at I plus 1, and the least significant digit at I plus 2. (In other words, take the decimal representation of VX, place the hundreds digit in memory at location in I, the tens digit at location I+1, and the ones digit at location I+2.) 
							vx = registers[opcode & 0x0F00 >> 8];

							memory[indexRegister + 0] = (byte)(vx / 100);
							memory[indexRegister + 1] = (byte)((vx / 10) % 10);
							memory[indexRegister + 2] = (byte)((vx % 100) % 100);


							break;
						case 0x0055: // (FX55) Stores V0 to VX (including VX) in memory starting at address I. The offset from I is increased by 1 for each value written, but I itself is left unmodified.
							count = (byte)((opcode & 0x0F00) >> 8);
							address = indexRegister;

							for (int i = 0; i < count + 1; i++)
								memory[address + i] = registers[i];
								
							break;
						case 0x0065: // (FX65) Fills V0 to VX (including VX) with values from memory starting at address I. The offset from I is increased by 1 for each value written, but I itself is left unmodified.
							count = (byte)((opcode & 0x0F00) >> 8);
							address = indexRegister;

							for (int i = 0; i < count + 1; i++)
								registers[i] = memory[address + i];

							break;
						case 0x00FF: // (FNFF) Custom: Prints N to console
							Console.WriteLine(string.Format("Output: 0x{0}", ((opcode & 0x0F00) >> 8).ToString("X1")));
							break;
						default:
							UnknownOpcode();
							break;
					}
					break;
				default:
					UnknownOpcode();
					break;
			}


			if (incrementPC)
				programCounter += 2;

			// Fetch opcode

			// Decode opcode

			// Execute opcode

			// Run timers
		}

		private void UnknownOpcode()
		{
			Console.WriteLine(string.Format("Unknown opcode: 0x{0}", opcode.ToString("X4")));
		}

		public void FetchOpcode()
		{
			short upper = memory[programCounter];
			short lower = memory[programCounter + 1];

			upper <<= 8;

			opcode = (short)(upper + lower);
		}

		public void DumpMemory()
		{
			string dump = "";

			for (int i = 0; i < memory.Length; i++)
			{
				if (i % 16 == 0)
					dump += i.ToString("X4") + "\t";

				dump += memory[i].ToString("X2");

				if ((i + 1) % 16 == 0)
					dump += "\n";
				else
					dump += " ";
			}

			Console.WriteLine(dump);
		}

		public void DumpGfx()
		{
			string dump = "";

			for (int i = 0; i < gfx.Length; i++)
			{
				if (i % 16 == 0)
					dump += i.ToString("X4") + "\t";

				dump += gfx[i].ToString("X2");

				if ((i + 1) % 16 == 0)
					dump += "\n";
				else
					dump += " ";
			}

			Console.WriteLine(dump);
		}
	}
}
