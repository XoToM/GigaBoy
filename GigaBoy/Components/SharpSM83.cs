using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components
{
	[Flags]
	public enum InterruptType : byte { VBlank=1,Stat=2,Timer=4,Serial=8,Joypad=16 }
	public enum CPUMode : byte { Stopped,Running,Halted, LowPowerStop }
	public class SharpSM83 : ClockBoundDevice
	{
		public int DelayTicks { get; protected set; } = 0;
		public GBInstance GB { get; init; }
		public bool Running { get; set; } = true;
		public CPUMode CPUMode { get; protected set; } = CPUMode.Stopped;
		private bool __debug__ = true;
		public bool Debug { get => __debug__ && GBInstance.DEBUG; set => __debug__ = value; }
		public bool PrintOperation { get; set; } = false;
		public bool InstructionBreakpoints { get; set; } = false;
		public byte LastOpcode { get; protected set; } = 0;
		public short DmaCounter = 0;
		public ushort LastPC { get; protected set; } = 0;
		protected IEnumerator<bool> InstructionProcessor;
		#region Registers
		public byte A { get; set; }     //The 8-bit register A
		public byte B { get; set; }      //The 8-bit register B
		public byte C { get; set; }     //The 8-bit register C
		public byte D { get; set; }     //The 8-bit register D
		public byte E { get; set; }     //The 8-bit register E
		public byte F                   //The 8-bit flag register F
		{
			get
			{
				// Combine all the different flag Booleans into a single 8-bit value
				var data = Zero ? 128 : 0;
				data |= SubFlag ? 64 : 0;
				data |= HalfCarry ? 32 : 0;
				return (byte)(data | (Carry ? 16 : 0));
			}
			set
			{
				// Split the given value apart into individual flag bits, and assign them to their respective Booleans
				Zero = (value & 128) != 0;
				SubFlag = (value & 64) != 0;
				HalfCarry = (value & 32) != 0;
				Carry = (value & 16) != 0;
			}
		}
		public byte H { get; set; }   //The 8-bit register H
		public byte L { get; set; }   //The 8-bit register L
		public ushort HL                // The 16-bit register pair HL
		{
			get
			{
				// Join the registers H and L into a 16-bit value
				return (ushort)((H << 8) | L);  
			}
			set {
				//Mask out and set the value of the H register from the given value.
				H = (byte)((value & 0xFF00) >> 8);

				//Mask out and set the value of the L register from the given value. 
				L = (byte)(value & 0xFF);           
			}
		}
		public ushort BC
		{
			get
			{
				return (ushort)((B << 8) | C);
			}
			set { B = (byte)((value & 0xFF00) >> 8); C = (byte)(value & 0xFF); }
		}
		public ushort DE
		{
			get
			{
				return (ushort)((D << 8) | E);
			}
			set { D = (byte)((value & 0xFF00) >> 8); E = (byte)(value & 0xFF); }
		}
		public ushort AF
		{
			get
			{
				return (ushort)((A << 8) | F);
			}
			set { A = (byte)((value & 0xFF00) >> 8); F = (byte)(value & 0xFF); }
		}
		public ushort SP { get; set; }    //The 16-bit register SP (Stack Pointer)
		public ushort PC { get; set; }    //The 16-bit register PC (Program Counter)
		public bool Zero { get; set; }    // The zero bit of the Flag register.
		public bool Carry { get; set; }
		public bool SubFlag { get; set; }
		public bool HalfCarry { get; set; }
		public bool InterruptMasterEnable { get; set; }
		public InterruptType InterruptEnable { get; set; }
		public InterruptType InterruptFlags { get; set; }
		#endregion
		public SharpSM83(GBInstance gb)
		{
			GB = gb;
			InstructionProcessor = Processor().GetEnumerator();
			PC = 0x0100;
			SP = 0xFFFE;
			A = 0x01;
			BC = 0xFF13;
			DE = 0x00C1;
			HL = 0x8403;
			F = 0;
			var abc = (JObject?)JObject.Parse(System.IO.File.ReadAllText(Environment.CurrentDirectory + @"\GigaBoyTests\opcodes.json"))["unprefixed"];
			if (abc == null) throw new NullReferenceException();
			OpcodeDictionary = abc;
		}
		/// <summary>
		/// Processes a single tick of the cpu. CPU performs an action every 4 ticks, so most of the time this returns immediately. Returns true if an instruction has been successfully executed during this tick.
		/// </summary>
		/// <returns>true if an instruction has finished executing during this tick, false otherwise.</returns>
		public bool TickOnce()
		{
			try
			{
				if (DmaCounter <= 0)
				{
					DmaCounter = 0;
					GB.PPU.DmaBlock = false;
				}
				else
				{
					--DmaCounter;
				}

				if (DelayTicks-- <= 0)
				{
					DelayTicks = 3;
					if (!Running)
					{
						CPUMode = CPUMode.Stopped;
						return false;
					}
					return InstructionProcessor.MoveNext();
				}
				return false;
			}
			catch (Exception e)
			{
				if (e.StackTrace is not null) GB.Log(e.StackTrace);
				GB.Error(e);
			}
			return false;
		}
		/// <summary>
		/// Same as TickOnce, just without the return value. I wanted this class to implement the ClockBoundDevice class, and the interface wants void.
		/// </summary>
		public void Tick()
		{
			TickOnce();
		}
		public static JObject? OpcodeDictionary { get; set; }
		protected IEnumerable<bool> Processor()
		{
			string s;
			while (true)
			{
				CPUMode = CPUMode.Running;
				byte opcode = Fetch();
			Execute://I don't like using goto either, but I think goto will be most effective here. Its here so I can skip the instruction fetch.
				LastOpcode = opcode;
				LastPC = PC;

				switch (opcode & 0b11000000)
				{
					case 0:
						byte data;
						sbyte sdata;
						ushort address;
						int idata;
						switch (opcode & 0b00111111)
						{
							case 0:
								break;
							case 1:
								yield return false;
								data = Fetch();
								yield return false;
								C = data;
								B = Fetch();
								break;
							case 2:
								yield return false;
								Store(BC, A);
								break;
							case 3:
								yield return false;
								++BC;
								break;

							case 4:
								SubFlag = false;
								Zero = ++B == 0;
								HalfCarry = (B & 15) == 0;
								break;
							case 5:
								SubFlag = true;
								Zero = --B == 0;
								HalfCarry = (B & 15) == 15;
								break;
							case 6:
								yield return false;
								B = Fetch();
								break;
							case 7:
								idata = (A & 128) >> 7;
								A = (byte)((A << 1) | idata);
								Zero = false;
								HalfCarry = false;
								SubFlag = false;
								Carry = idata != 0;
								break;

							case 8:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)((Fetch() << 8) | data);//Wrong?
								yield return false;
								Store(address++, (byte)(SP & 0x00FF));
								yield return false;
								Store(address, (byte)((SP & 0xFF00) >> 8));
								break;
							case 9:
								data = L;
								idata = HL + BC;
								yield return false;
								HL = (ushort)(idata & 0xFFFF);
								HalfCarry = L < data;
								Carry = (idata & 0xFFFF0000) != 0;
								SubFlag = false;
								break;
							case 0xA:
								yield return false;
								A = Fetch(BC);
								break;
							case 0xB:
								yield return false;
								--BC;
								break;

							case 0xC:
								SubFlag = false;
								Zero = ++C == 0;
								HalfCarry = (C & 15) == 0;
								break;
							case 0xD:
								SubFlag = true;
								Zero = --C == 0;
								HalfCarry = (C & 15) == 15;
								break;
							case 0xE:
								yield return false;
								C = Fetch();
								break;
							case 0xF:
								idata = (A & 1) << 7;
								A = (byte)((A >> 1) | idata);
								Zero = false;
								HalfCarry = false;
								SubFlag = false;
								Carry = idata == 1;
								break;


							case 0x10:
								CPUMode = CPUMode.LowPowerStop;
								GB.Timers.ResetDIV();
								throw new NotImplementedException("STOP instruction has not been implemented yet.");
							case 0x11:
								yield return false;
								data = Fetch();
								yield return false;
								E = data;
								D = Fetch();
								break;
							case 0x12:
								yield return false;
								Store(DE, A);
								break;
							case 0x13:
								yield return false;
								++DE;
								break;

							case 0x14:
								SubFlag = false;
								Zero = ++D == 0;
								HalfCarry = (D & 15) == 0;
								break;
							case 0x15:
								SubFlag = true;
								Zero = --D == 0;
								HalfCarry = (D & 15) == 15;
								break;
							case 0x16:
								yield return false;
								D = Fetch();
								break;
							case 0x17:
								idata = A & 128;
								A = (byte)((A << 1) | (Carry ? 1 : 0));
								Zero = false;
								HalfCarry = false;
								SubFlag = false;
								Carry = idata != 0;
								break;

							case 0x18:
								yield return false;
								sdata = (sbyte)Fetch();
								yield return false;
								Jump((ushort)(PC + sdata));
								break;
							case 0x19:
								address = HL;
								data = L;
								idata = HL + DE;
								yield return false;
								HL = (ushort)(idata & 0xFFFF);
								HalfCarry = L < data;
								Carry = (idata & 0xFFFF0000) != 0;
								SubFlag = false;
								break;
							case 0x1A:
								yield return false;
								A = Fetch(DE);
								break;
							case 0x1B:
								yield return false;
								--DE;
								break;

							case 0x1C:
								SubFlag = false;
								Zero = ++E == 0;
								HalfCarry = (E & 15) == 0;
								break;
							case 0x1D:
								SubFlag = true;
								Zero = --E == 0;
								HalfCarry = (E & 15) == 15;
								break;
							case 0x1E:
								yield return false;
								E = Fetch();
								break;
							case 0x1F:
								idata = A & 1;
								A = (byte)((A >> 1) | (Carry ? 128 : 0));
								Zero = false;
								HalfCarry = false;
								SubFlag = false;
								Carry = idata != 0;
								break;


							case 0x20:
								yield return false;
								sdata = (sbyte)Fetch();
								if (!Zero)
								{
									yield return false;
									Jump((ushort)(PC + sdata));
								}
								break;
							case 0x21:
								yield return false;
								data = Fetch();
								yield return false;
								L = data;
								H = Fetch();
								break;
							case 0x22:
								yield return false;
								Store(HL++, A);
								break;
							case 0x23:
								yield return false;
								++HL;
								break;

							case 0x24:
								SubFlag = false;
								Zero = ++H == 0;
								HalfCarry = (H & 15) == 0;
								break;
							case 0x25:
								SubFlag = true;
								Zero = --H == 0;
								HalfCarry = (H & 15) == 15;
								break;
							case 0x26:
								yield return false;
								H = Fetch();
								break;
							case 0x27://DAA ?????
								data = 0;
								if (HalfCarry || (A & 0xF) > 9) data = 6;
								if (Carry || (A & 0xF0) > 0x90) data |= 6;
								idata = A + data;
								A = (byte)idata;
								HalfCarry = false;
								Carry = (idata & 0xFFFFFF00) != 0;
								Zero = A == 0;
								break;

							case 0x28:
								yield return false;
								sdata = (sbyte)Fetch();
								if (Zero)
								{
									yield return false;
									Jump((ushort)(PC + sdata));
								}
								break;
							case 0x29:
								address = HL;
								data = L;
								idata = HL + HL;
								yield return false;
								HL = (ushort)(idata & 0xFFFF);
								HalfCarry = L < data;
								Carry = (idata & 0xFFFF0000) != 0;
								SubFlag = false;
								break;
							case 0x2A:
								yield return false;
								A = Fetch(HL);
								++HL;
								break;
							case 0x2B:
								yield return false;
								--HL;
								break;

							case 0x2C:
								SubFlag = false;
								Zero = ++L == 0;
								HalfCarry = (L & 15) == 0;
								break;
							case 0x2D:
								SubFlag = true;
								Zero = --L == 0;
								HalfCarry = (L & 15) == 15;
								break;
							case 0x2E:
								yield return false;
								L = Fetch();
								break;
							case 0x2F:
								A = (byte)~A;
								HalfCarry = true;
								SubFlag = true;
								break;



							case 0x30:
								yield return false;
								sdata = (sbyte)Fetch();
								if (!Carry)
								{
									yield return false;
									Jump((ushort)(PC + sdata));
								}
								break;
							case 0x31:
								yield return false;
								data = Fetch();
								yield return false;
								SP = (ushort)((Fetch() << 8) | data);
								break;
							case 0x32:
								yield return false;
								Store(HL--, A);
								break;
							case 0x33:
								yield return false;
								++SP;
								break;

							case 0x34:
								yield return false;
								data = Fetch(HL);
								yield return false;
								Store(HL, ++data);
								SubFlag = false;
								Zero = data == 0;
								HalfCarry = (data & 15) == 0;
								break;
							case 0x35:
								yield return false;
								data = Fetch(HL);
								yield return false;
								Store(HL, --data);
								SubFlag = true;
								Zero = data == 0;
								HalfCarry = (data & 15) == 15;
								break;
							case 0x36:
								yield return false;
								data = Fetch();
								yield return false;
								Store(HL, data);
								break;
							case 0x37:
								Carry = true;
								HalfCarry = false;
								SubFlag = false;
								break;

							case 0x38:
								yield return false;
								sdata = (sbyte)Fetch();
								if (Carry)
								{
									yield return false;
									Jump((ushort)(PC + sdata));
								}
								break;
							case 0x39:
								data = L;
								idata = HL + SP;
								yield return false;
								HL = (ushort)(idata & 0xFFFF);
								HalfCarry = L < data;
								Carry = (idata & 0xFFFF0000) != 0;
								SubFlag = false;
								break;
							case 0x3A:
								yield return false;
								A = Fetch(HL--);
								break;
							case 0x3B:
								yield return false;
								--SP;
								break;

							case 0x3C:
								SubFlag = false;
								Zero = ++A == 0;
								HalfCarry = (A & 15) == 0;
								break;
							case 0x3D:
								SubFlag = true;
								Zero = --A == 0;
								HalfCarry = (L & 15) == 15;
								break;
							case 0x3E:
								yield return false;
								A = Fetch();
								break;
							case 0x3F:
								Carry = !Carry;
								HalfCarry = false;
								SubFlag = false;
								break;

							default:
								throw new NotImplementedException($"Invalid Opcode {opcode:X}");
						}
						break;
					case 64:
						byte val;
						var source = opcode & 7;
						var dest = (opcode >> 3) & 7;
						if (source == dest && source == 6)
						{
							if (InterruptMasterEnable)//HALT
							{
								CPUMode = CPUMode.Halted;
								while (InterruptFlags == 0)
								{
									yield return false;
								}
								CPUMode = CPUMode.Running;
								break;
							}
							else
							{
								yield return true;
								opcode = Fetch();
								--PC;
								goto Execute;
							}//HALT
						}
						switch (source)
						{
							case 0:
								val = B;
								break;
							case 1:
								val = C;
								break;
							case 2:
								val = D;
								break;
							case 3:
								val = E;
								break;
							case 4:
								val = H;
								break;
							case 5:
								val = L;
								break;
							case 6:
								yield return false;
								val = Fetch(HL);
								break;
							case 7:
								val = A;
								break;
							default:
								throw new NotImplementedException();
						}
						switch (dest)
						{
							case 0:
								B = val;
								break;
							case 1:
								C = val;
								break;
							case 2:
								D = val;
								break;
							case 3:
								E = val;
								break;
							case 4:
								H = val;
								break;
							case 5:
								L = val;
								break;
							case 6:
								yield return false;
								Store(HL, val);
								break;
							case 7:
								A = val;
								break;
							default:
								throw new NotImplementedException();
						}
						if (source == dest && Debug && InstructionBreakpoints)
						{
							GB.BreakpointHit();
							System.Diagnostics.Debug.WriteLine("Instruction Breakpoint");
						}
						break;
					case 128:
						switch (opcode & 7)
						{
							case 0:
								data = B;
								break;
							case 1:
								data = C;
								break;
							case 2:
								data = D;
								break;
							case 3:
								data = E;
								break;
							case 4:
								data = H;
								break;
							case 5:
								data = L;
								break;
							case 6:
								yield return false;
								data = Fetch(HL);
								break;
							case 7:
								data = A;
								break;
							default:
								throw new NotImplementedException($"Invalid Opcode {opcode:X}");
						}
						switch (opcode & 0b00111000)
						{
							case 0:
								idata = A + data;
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) < (A & 0xF);
								A = (byte)idata;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 8:
								idata = A + data + (Carry ? 1 : 0);
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) < (A & 0xF);
								A = (byte)idata;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 16:
								idata = A - data;
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) > (A & 0xF);
								A = (byte)idata;
								Zero = A == 0;
								SubFlag = true;
								break;
							case 24:
								idata = A - data - (Carry ? 1 : 0);
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) > (A & 0xF);
								A = (byte)idata;
								Zero = A == 0;
								SubFlag = true;
								break;
							case 32:
								A = (byte)(A & data);
								Carry = false;
								HalfCarry = true;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 40:
								A = (byte)(A ^ data);
								Carry = false;
								HalfCarry = false;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 48:
								A = (byte)(A | data);
								Carry = false;
								HalfCarry = false;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 56:
								idata = A - data;
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) > (A & 0xF);
								Zero = A == data;
								SubFlag = true;
								break;
							default:
								throw new NotImplementedException($"Invalid Opcode {opcode:X}");
						}
						break;
					case 192:
						switch (opcode & 0b00111111)
						{
							case 0:
								yield return false;
								if (Zero) break;
								yield return false;
								data = Fetch(SP++);
								yield return false;
								Jump((ushort)((Fetch(SP++) << 8) | data));
								yield return false;
								break;
							case 1:
								yield return false;
								data = Fetch(SP++);
								yield return false;
								BC = (ushort)((Fetch(SP++) << 8) | data);
								break;
							case 2:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)(data | (Fetch() << 8));
								if (!Zero)
								{
									yield return false;
									Jump(address);
								}
								break;
							case 3:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)(data | (Fetch() << 8));
								yield return false;
								Jump(address);
								break;
							case 4:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)(data | (Fetch() << 8));
								if (!Zero)
								{
									yield return false;
									yield return false;
									Store(--SP, (byte)(((PC + 3) & 0xFF00) >> 8));
									yield return false;
									Store(--SP, (byte)((PC + 3) & 0xFF));
									Jump(address);
								}
								break;
							case 5:
								yield return false;
								yield return false;
								Store(--SP, B);
								yield return false;
								Store(--SP, C);
								break;
							case 6:
								yield return false;
								data = Fetch();
								idata = A + data;
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) < (A & 0xF);
								A = (byte)idata;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 7:
								yield return false;
								address = (ushort)(opcode & 0b00111000);
								yield return false;
								Store(--SP, (byte)((PC & 0xFF00) >> 8));
								yield return false;
								Store(--SP, (byte)(PC & 0xFF));
								Jump(address);
								break;

							case 8:
								yield return false;
								if (!Zero) break;
								yield return false;
								data = Fetch(SP++);
								yield return false;
								Jump((ushort)((Fetch(SP++) << 8) | data));
								yield return false;
								break;
							case 9:
								yield return false;
								data = Fetch(SP++);
								yield return false;
								address = (ushort)((Fetch(SP++) << 8) | data);
								yield return false;
								Jump(address);
								break;
							case 0xA:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)(data | (Fetch() << 8));
								if (Zero)
								{
									yield return false;
									Jump(address);
								}
								break;
							case 0xB:   //Prefix 0xCB command
								yield return false;
								byte operand = Fetch();
								switch (operand & 7)
								{
									case 0:
										data = B;
										break;
									case 1:
										data = C;
										break;
									case 2:
										data = D;
										break;
									case 3:
										data = E;
										break;
									case 4:
										data = H;
										break;
									case 5:
										data = L;
										break;
									case 6:
										yield return false;
										data = Fetch(HL);
										break;
									case 7:
										data = A;
										break;
									default:
										throw new NotImplementedException($"Invalid Opcode CB{operand:X}");
								}

								int mask = 1 << ((operand >> 3) & 7);

								switch (operand & 0b11000000)
								{
									case 0:
										switch (operand & 0b00111000)
										{
											case 0:
												idata = (data & 128) >> 7;
												data = (byte)((data << 1) | idata);
												Zero = data == 0;
												HalfCarry = false;
												SubFlag = false;
												Carry = idata != 0;
												break;
											case 8:
												idata = (data & 1) << 7;
												data = (byte)((data >> 1) | idata);
												Zero = data == 0;
												HalfCarry = false;
												SubFlag = false;
												Carry = idata != 0;
												break;
											case 16:
												idata = data & 128;
												data = (byte)((data << 1) | (Carry ? 1 : 0));
												Zero = data == 0;
												HalfCarry = false;
												SubFlag = false;
												Carry = idata != 0;
												break;
											case 24:    //  Incorrectly Implemented Instruction?    //RR {reg}
												idata = data & 1;
												data = (byte)((data >> 1) | (Carry ? 0x80 : 0));
												Zero = data == 0;
												HalfCarry = false;
												SubFlag = false;
												Carry = idata != 0;
												break;
											case 32:
												SubFlag = false;
												HalfCarry = false;
												Carry = (data & 0b10000000) != 0;
												data = (byte)(data << 1);
												Zero = data == 0;
												break;
											case 40:
												SubFlag = false;
												HalfCarry = false;
												Carry = (data & 1) != 0;
												idata = data & 0b10000000;
												data = (byte)((data >> 1) | idata);
												Zero = data == 0;
												break;
											case 48:
												SubFlag = false;
												HalfCarry = false;
												Carry = false;
												Zero = data == 0;
												data = (byte)((data << 4) | (data >> 4));
												break;
											case 56:
												SubFlag = false;
												HalfCarry = false;
												Carry = (data & 1) != 0;
												data = (byte)(data >> 1);
												break;
											default:
												throw new NotImplementedException($"Invalid Opcode CB{operand:X}");
										}
										break;
									case 64:
										SubFlag = false;
										HalfCarry = true;
										Zero = (mask & data) == 0;
										break;
									case 128:
										data = (byte)(data & (~mask));
										break;
									case 192:
										data = (byte)(data | mask);
										break;
								}

								if ((operand & 0b11000000) != 0b01000000)
								{
									switch (operand & 7)
									{
										case 0:
											B = data;
											break;
										case 1:
											C = data;
											break;
										case 2:
											D = data;
											break;
										case 3:
											E = data;
											break;
										case 4:
											H = data;
											break;
										case 5:
											L = data;
											break;
										case 6:
											yield return false;
											Store(HL, data);
											break;
										case 7:
											A = data;
											break;
										default:
											throw new NotImplementedException($"Invalid Opcode CB{operand:X}");
									}
								}
								break;
							case 0xC:   //Opcode 0xCC =  CALL Z, a16     - conditional subroutine call: if the zero flag is set store the current address on the stack and jump execution to the given address

								yield return false; //Pause for one CPU cycle

								data = Fetch();                                     //Fetch the least significant byte of the address. If no arguments are given the Fetch function uses the address indicated by PC to fetch the byte. It will also increment the PC register

								yield return false; //Pause for one CPU cycle

								address = (ushort)((Fetch() << 8) | data);          //Fetch the most significant byte of the address and combine it with the least significant byte
								
								if (Zero)               //Check if the zero flag is set
								{                       //If it is then we proceed with the jump
									yield return false; //Pause for two CPU cycles so the timings are correct
									yield return false;

									Store(--SP, (byte)(((PC + 3) & 0xFF00) >> 8));  //Store the current address on the stack so we can return here later

									yield return false; //Pause for one CPU cycle

									Store(--SP, (byte)((PC + 3) & 0xFF));           //Store the current address on the stack so we can return here later
									Jump(address);                                  //Jump to the given address.
									
								}
								break;
							case 0xD:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)((Fetch() << 8) | data);
								yield return false;
								yield return false;
								Store(--SP, (byte)(((PC) & 0xFF00) >> 8));
								yield return false;
								Store(--SP, (byte)((PC) & 0xFF));
								Jump(address);
								break;
							case 0xE:
								yield return false;
								data = Fetch();
								idata = A + data + (Carry ? 1 : 0);
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) < (A & 0xF);
								A = (byte)idata;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 0xF:
								yield return false;
								address = (ushort)(opcode & 0b00111000);
								yield return false;
								Store(--SP, (byte)((PC & 0xFF00) >> 8));
								yield return false;
								Store(--SP, (byte)(PC & 0xFF));
								Jump(address);
								break;


							case 0x10:
								yield return false;
								if (Carry) break;
								yield return false;
								data = Fetch(SP++);
								yield return false;
								Jump((ushort)((Fetch(SP++) << 8) | data));
								yield return false;
								break;
							case 0x11:
								yield return false;
								data = Fetch(SP++);
								yield return false;
								DE = (ushort)((Fetch(SP++) << 8) | data);
								break;
							case 0x12:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)((Fetch() << 8) | data);
								if (!Carry)
								{
									yield return false;
									Jump(address);
								}
								break;
							case 0x14:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)((Fetch() << 8) | data);
								if (!Carry)
								{
									yield return false;
									yield return false;
									Store(--SP, (byte)(((PC + 3) & 0xFF00) >> 8));
									yield return false;
									Store(--SP, (byte)((PC + 3) & 0xFF));
									Jump(address);
								}
								break;
							case 0x15:
								yield return false;
								yield return false;
								Store(--SP, D);
								yield return false;
								Store(--SP, E);
								break;
							case 0x16:
								yield return false;
								data = Fetch();
								idata = A - data;
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) > (A & 0xF);
								A = (byte)idata;
								Zero = A == 0;
								SubFlag = true;
								break;
							case 0x17:
								yield return false;
								address = (ushort)(opcode & 0b00111000);
								yield return false;
								Store(--SP, (byte)((PC & 0xFF00) >> 8));
								yield return false;
								Store(--SP, (byte)(PC & 0xFF));
								Jump(address);
								break;

							case 0x18:
								yield return false;
								GB.Log($"Carry status: {Carry}");
								if (!Carry) break;
								yield return false;
								data = Fetch(SP++);
								yield return false;
								Jump((ushort)((Fetch(SP++) << 8) | data));
								yield return false;
								break;
							case 0x19:
								yield return false;
								data = Fetch(SP++);
								yield return false;
								address = (ushort)((Fetch(SP++) << 8) | data);
								yield return false;
								Jump(address);
								InterruptMasterEnable = true;
								break;
							case 0x1A:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)((Fetch() << 8) | data);
								if (Carry)
								{
									yield return false;
									Jump(address);
								}
								break;
							case 0x1C:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)((Fetch() << 8) | data);
								if (Carry)
								{
									yield return false;
									yield return false;
									Store(--SP, (byte)(((PC + 3) & 0xFF00) >> 8));
									yield return false;
									Store(--SP, (byte)((PC + 3) & 0xFF));
									Jump(address);
								}
								break;
							case 0x1E:
								yield return false;
								data = Fetch();
								idata = A - data - (Carry ? 1 : 0);
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) > (A & 0xF);
								A = (byte)idata;
								Zero = A == 0;
								SubFlag = true;
								break;
							case 0x1F:
								yield return false;
								address = (ushort)(opcode & 0b00111000);
								yield return false;
								Store(--SP, (byte)((PC & 0xFF00) >> 8));
								yield return false;
								Store(--SP, (byte)(PC & 0xFF));
								Jump(address);
								break;


							case 0x20:  //LDH (a8), A
								yield return false;
								address = (ushort)(Fetch() | 0xFF00);
								yield return false;
								Store(address, A);
								break;
							case 0x21:
								yield return false;
								data = Fetch(SP++);
								yield return false;
								HL = (ushort)((Fetch(SP++) << 8) | data);
								break;
							case 0x22:
								yield return false;
								Store((ushort)(0xFF00 | C), A);
								break;
							case 0x25:
								yield return false;
								yield return false;
								Store(--SP, H);
								yield return false;
								Store(--SP, L);
								break;
							case 0x26:
								yield return false;
								data = Fetch();
								A = (byte)(A & data);
								Carry = false;
								HalfCarry = true;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 0x27:
								yield return false;
								address = (ushort)(opcode & 0b00111000);
								yield return false;
								Store(--SP, (byte)((PC & 0xFF00) >> 8));
								yield return false;
								Store(--SP, (byte)(PC & 0xFF));
								Jump(address);
								break;

							case 0x28:      //ADD SP, signed const byte
								yield return false;
								sdata = (sbyte)Fetch();
								yield return false;
								Zero = false;
								SubFlag = false;
								Carry = (((SP & 0xFF) + sdata) & 0xFFFFFF00) != 0;
								HalfCarry = (((SP & 0xF) + (sdata & 0xF)) & 0xFFFFFFF0) != 0;
								yield return false;
								SP = (ushort)(SP + sdata);
								break;
							case 0x29:
								Jump(HL);
								break;
							case 0x2A:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)((Fetch() << 8) | data);
								yield return false;
								Store(address, A);
								break;
							case 0x2E:
								yield return false;
								data = Fetch();
								A = (byte)(A ^ data);
								Carry = false;
								HalfCarry = false;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 0x2F:
								yield return false;
								address = (ushort)(opcode & 0b00111000);
								yield return false;
								Store(--SP, (byte)((PC & 0xFF00) >> 8));
								yield return false;
								Store(--SP, (byte)(PC & 0xFF));
								Jump(address);
								break;

							case 0x30:
								yield return false;
								address = (ushort)(0xFF00 | Fetch());
								yield return false;
								A = Fetch(address);
								break;
							case 0x31:
								yield return false;
								data = Fetch(SP++);
								yield return false;
								AF = (ushort)((Fetch(SP++) << 8) | data);
								break;
							case 0x32:
								yield return false;
								address = (ushort)(0xFF00 | C);
								A = Fetch(address);
								break;
							case 0x33:
								InterruptMasterEnable = false;
								break;
							case 0x35:
								yield return false;
								yield return false;
								Store(--SP, A);
								yield return false;
								Store(--SP, F);
								break;
							case 0x36:
								yield return false;
								data = Fetch();
								A = (byte)(A | data);
								Carry = false;
								HalfCarry = false;
								Zero = A == 0;
								SubFlag = false;
								break;
							case 0x37:
								yield return false;
								address = (ushort)(opcode & 0b00111000);
								yield return false;
								Store(--SP, (byte)((PC & 0xFF00) >> 8));
								yield return false;
								Store(--SP, (byte)(PC & 0xFF));
								Jump(address);
								break;

							case 0x38:      //LD HL, SP + signed byte
								yield return false;
								sdata = (sbyte)Fetch();
								yield return false;
								Zero = false;
								SubFlag = false;
								Carry = (((SP & 0xFF) + sdata) & 0xFFFFFF00) != 0;
								HalfCarry = (((SP & 0xF) + (sdata & 0xF)) & 0xFFFFFFF0) != 0;
								HL = (ushort)(SP + sdata);
								break;
							case 0x39:
								yield return false;
								SP = HL;
								break;
							case 0x3A:
								yield return false;
								data = Fetch();
								yield return false;
								address = (ushort)(data | (Fetch() << 8));
								yield return false;
								A = Fetch(address);
								break;
							case 0x3B:   //EI
								InterruptMasterEnable = true;
								yield return true;
								continue;       //the EI instruction is bugged: The processor won't handle any interrupts until the instruction after EI is done executing. This means that if the DI instruction is executed directly after EI no interrupts will be handled, even if there are interrupts scheduled. To emulate this behaviour I made the EI instruction jump to the start of the main loop, as the interrupts are handled at the end of it.
							case 0x3E:
								yield return false;
								data = Fetch();
								idata = A - data;
								Carry = (idata & 0xFFFFFF00) != 0;
								HalfCarry = (idata & 0xF) > (A & 0xF);
								Zero = A == data;
								SubFlag = true;
								break;
							case 0x3F:
								yield return false;
								address = (ushort)(opcode & 0b00111000);
								yield return false;
								Store(--SP, (byte)((PC & 0xFF00) >> 8));
								yield return false;
								Store(--SP, (byte)(PC & 0xFF));
								Jump(address);
								break;

							default:
								throw new NotImplementedException($"Invalid Opcode {opcode:X}");
						}
						break;
					default:
						throw new NotImplementedException($"Invalid Opcode {opcode:X}");
				}
				if (Debug && PrintOperation)
				{
					if (OpcodeDictionary is not null)//This mess of if statements is not pretty, but visual studio complains a lot here, and I had to clear up my warnings list. 
					{//I could also have used #nullable disabled to disable these warnings, but this could also lead to me missing a NullReferenceException so I decided to go with this approach. I will most likely delete this debug code from the final build anyway, so this doesn't really matter anyway.
						s = opcode.ToString("X").ToUpper();
						if (s.Length < 2) s = "0" + s;
						if (s.Length < 2) s = "0" + s;
						var obj = (JObject?)OpcodeDictionary["0x" + s];
						if (obj is not null)
						{
							var mnem = obj["mnemonic"];
							if (mnem is not null)
							{
								s = mnem.ToString();
								var operandsList = obj["operands"];
								if (operandsList is not null)
								{
									foreach (JObject operandData in (JArray)operandsList)
									{
										var opcodeName = operandData["name"];
										if (opcodeName is not null)
											s += " " + opcodeName.ToString();
									}

									GB.Log('[' + LastPC.ToString("X") + ']' + ' ' + s);
								}
							}
						}
					}
				}
				yield return true;  //Mark the current instruction as finished, and pause for 1 last cycle
				
				//Check if any interrupts are enqueued, and interrupt handling is on
				if (InterruptMasterEnable && (((int)InterruptEnable & (int)InterruptFlags & 0x1F) != 0))
				{
					ushort address;
					var interrupts = InterruptEnable & InterruptFlags;
					if (interrupts.HasFlag(InterruptType.VBlank)) {				//Mark the VBlank interrupt as handled
						InterruptFlags = interrupts & ~InterruptType.VBlank;
						address = 0x0040;
					} else if (interrupts.HasFlag(InterruptType.Stat)) {		//Mark the STAT interrupt as handled
						InterruptFlags = interrupts & ~InterruptType.Stat;
						address = 0x0048;
					} else if (interrupts.HasFlag(InterruptType.Timer)) {       //Mark the Timer interrupt as handled
						InterruptFlags = interrupts & ~InterruptType.Timer;
						address = 0x0050;
					} else if (interrupts.HasFlag(InterruptType.Serial)) {      //Mark the Serial interrupt as handled
						InterruptFlags = interrupts & ~InterruptType.Serial;
						address = 0x0058;
					} else {													//Mark the Joypad interrupt as handled
						InterruptFlags = interrupts & ~InterruptType.Joypad;
						address = 0x0060;
					}

					InterruptMasterEnable = false;				//	Disable interrupts
					
					yield return false;							//	Wait for 2 CPU cycles so the timings are correct.
					yield return false;

					Store(--SP, (byte)((PC & 0xFF00) >> 8));	//	Store the current address on the stack
					yield return false;
					Store(--SP, (byte)(PC & 0xFF));
					yield return false;

					Jump(address);      //	Jump execution to the interrupt service routine and notify the emulator that the operation has been completed successfully
					yield return true;
				}
			}
		}
		protected byte Fetch(ushort address, bool checkBreakpoints = true)
		{
			if (checkBreakpoints && GB.Breakpoints.ContainsKey(address) && GBInstance.DEBUG)
			{
				LinkedList<BreakpointInfo>? breakpoints = GB.Breakpoints[address];
				if (breakpoints == null || breakpoints.Count == 0)
				{
					GB.Breakpoints.Remove(address);
				}
				else
				{
					foreach (var breakpoint in breakpoints)
					{
						if (breakpoint.BreakOnRead)
						{
							GB.BreakpointHit();
							break;
						}
					}
				}
			}
			return GetByte(address);
		}
		protected void Store(ushort address, byte value)
		{
			//GB.Log($"[{address:X}] = {value:X}");
			if (GB.Breakpoints.TryGetValue(address, out LinkedList<BreakpointInfo>? breakpoints) && GBInstance.DEBUG)
			{
				if (breakpoints == null || breakpoints.Count == 0)
				{
					GB.Breakpoints.Remove(address);
				}
				else
				{
					foreach (var breakpoint in breakpoints)
					{
						if (breakpoint.BreakOnWrite)
						{
							GB.BreakpointHit();
							break;
						}
					}
				}
			}
			SetByte(address, value);
		}
		protected void Jump(ushort address)
		{
			if (GB.Breakpoints.TryGetValue(address, out LinkedList<BreakpointInfo>? breakpoints) && GBInstance.DEBUG)
			{
				if (breakpoints == null || breakpoints.Count == 0)
				{
					GB.Breakpoints.Remove(address);
				}
				else
				{
					foreach (var breakpoint in breakpoints)
					{
						if (breakpoint.BreakOnJump)
						{
							GB.BreakpointHit();
							break;
						}
					}
				}
			}
			PC = address;
		}
		// Fetch the next byte indicated by the Program Counter
		protected byte Fetch()
		{
			// Check if there is a breakpoint on this address, and if so, pause the emulator
			if (GB.Breakpoints.TryGetValue(PC, out LinkedList<BreakpointInfo>? breakpoints) && GBInstance.DEBUG)
			{
				if (breakpoints == null || breakpoints.Count == 0)
				{
					GB.Breakpoints.Remove(PC);
				}
				else
				{
					foreach (var breakpoint in breakpoints)
					{
						if (breakpoint.BreakOnExecute)
						{
							GB.BreakpointHit();
							break;
						}
					}
				}
			}
			// Store which address the last instruction was at. This is used for debugging and is displayed when a breakpoint is hit.
			LastPC = PC;
			// Fetch the byte from memory / rom / register and increment the Program Counter afterwards. Return the fetched byte
			return GetByte(PC++);
		}

		public void StartDMA(ushort source, ushort count, ushort destination) {
			DmaCounter = 160;
			GB.PPU.DmaBlock = true;
			for (int i = 0; i < count; i++) {
				SetByte((ushort)(destination + i),GetByte( (ushort)(source + i), false),false);
			}
		}

		// Gets a byte from the given address. If a DMA transfer is using the memory at that address return 0xFF
		protected byte GetByte(ushort address, bool checkDma=true)
		{
			// Check if a DMA transfer is using this address. If so, then return 0xFF
			if (checkDma && DmaCounter != 0 && address < 0xFF00) return 0xFF;

			// Retrieve and return the byte from memory 
			return GB.MemoryMapper.GetByte(address);
		}
		// Changes a memory mapped register/memory address. If a DMA transfer is using this address, then this is a NOP
		protected void SetByte(ushort address, byte value, bool checkDma=true)
		{
			//  Check if a dma transfer is using this address. If so, then return
			if (checkDma && DmaCounter != 0 && address < 0xFF00) return;
			//  Change the byte at the given address
			GB.MemoryMapper.SetByte(address, value);
		}
		public void SetInterrupt(InterruptType interrupt) {
			InterruptFlags = InterruptFlags | interrupt;
		}
	}
}
