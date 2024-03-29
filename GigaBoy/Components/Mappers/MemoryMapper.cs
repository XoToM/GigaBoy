﻿using GigaBoy.Components.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Mappers
{
	public class MemoryMapper : MMIODevice, IList<byte>      //This entire class (and its subclasses) are a huge mess which i will rewrite later.
	{
		public byte[] RomImage { get; set; }
		public GBInstance GB { get; protected set; }
		public RAM SRam { get; protected set; }
		public bool SaveSRam { get; set; } = false;
		public bool WasTilemap1Modified { get; set; } = false;
		public bool WasTilemap2Modified { get; set; } = false;
		public bool WasCharArea1Modified { get; set; } = false;
		public bool WasCharArea2Modified { get; set; } = false;
		public bool WasCharArea3Modified { get; set; } = false;
		public int SRamBankOffset { get; set; } = 0;

		public int Count => ushort.MaxValue + 1;

		public bool IsReadOnly => true;

		public byte this[int index] { get => GetByte((ushort)index); set => SetByte((ushort)index, value); }
		public byte[] SoundRegs = new byte[] { 0x80, 0xBF, 0xF3, 0xFF, 0xBF, 0xFF, 0x3F, 0x00, 0xFF, 0xBF, 0x7F, 0xFF, 0x9F, 0xFF, 0xBF, 0xFF, 0xFF, 0x00, 0x00, 0xBF, 0x77, 0xF3, 0xF1 };

		public MemoryMapper(GBInstance gb,byte[] romImage,bool battery) {
			RomImage = romImage;
			if (RomImage.Length < 0x8000)
			{
				gb.Log("Rom file is smaller than expected: Padding rom with 0x00 bytes. Real gameboy wouldn't care.");
				byte[] rom1 = new byte[0x8000];
				Array.Fill<byte>(rom1, 0);
				romImage.CopyTo(rom1.AsSpan());
				RomImage = rom1;
			}
			GB = gb;
			SaveSRam = battery;
			SRam = new(gb, GetSRamSize()) {Type = RAMType.SRAM };
		}
		public int GetSRamSize() {
			switch (RomImage[0x0149]) {
				case 0://Cartridge has no RAM. This emulator doesn't currently emulate the MDR register, so I will emulate the ram anyway. Its contents won't be saved.
				case 1://Unused, assuming the same behaviour as No Ram
					SaveSRam = false;
					return 0x2000;
				case 2:
					return 0x2000;
				case 3:
					return 0x8000;
				case 4:
					return 0x20000;
				case 5:
					return 0x10000;
			}
			return 0x2000;
		}
		public byte GetByte(ushort address,bool direct=false)
		{
			//GB.Log($"Read [{address:X}]");
			if (address < 0x8000) return Read(address);
			return StandardGetByte(address,direct);
		}
		public byte StandardGetByte(ushort address, bool direct) {
			if (address < 0x8000)   // This area of the memory map should be handled in the GetByte method
			{
				GB.Error("Invalid Read");
				return 0;
			}
			if (address < 0x9800)   //Check if the address points to the vram's charater data section.
			{
				var cram = GB.CRAMBanks[(address - 0x8000) / 0x800];
				return direct ? cram.DirectRead((ushort)((address - 0x8000) % 0x800)) : cram.Read((ushort)((address - 0x8000) % 0x800));
			}
			if (address < 0xA000)   //Check if the address points to the vram's map data section.
			{
				var tmram = GB.TMRAMBanks[(address - 0x9800) / 0x400];
				return direct ? tmram.DirectRead((ushort)((address - 0x9800) % 0x400)) : tmram.Read((ushort)((address - 0x9800) % 0x400));
			}
			if (address < 0xC000) return SRam.Read((ushort)(address - 0xA000 + SRamBankOffset));//Check if the address points to static ram.
			if (address < 0xE000) return GB.WRam.Read((ushort)(address - 0xC000));//Check if the address points to work ram.
			if (address < 0xFE00) return GB.WRam.Read((ushort)(address - 0xE000));//Check if the address points to echo work ram (some work ram's addresses are mapped twice. This copy is often called echo ram)
			if (address < 0xFF00) return GB.PPU.OAM.Read((ushort)(address - 0xFE00));//Check if the address points to OAM ram.

			if ((address & 0xFF00) == 0xFF00)
			{
				if (address >= 0xFF10 && address <= 0xFF26)
				{
					return SoundRegs[address - 0xFF10];
				}
				else {
					switch (address)
					{
						case 0xFF00:
							return GB.Joypad.Read((ushort)(address - 0xFF00));
						case 0xFF01:
							return 0x00;
						case 0xFF02:
							return 0x7E;
						case 0xFF04:
							return GB.Timers.GetDIV();
						case 0xFF05:
							return GB.Timers.GetTIMA();
						case 0xFF06:
							return GB.Timers.GetTMA();
						case 0xFF07:
							return GB.Timers.GetTAC();
						case 0xFF0F:
							return (byte)((int)GB.CPU.InterruptFlags | 0b11100000);
						case 0xFF40:
							return GB.PPU.LCDC;
						case 0xFF41:
							return GB.PPU.STAT;
						case 0xFF42:
							return GB.PPU.SCY;
						case 0xFF43:
							return GB.PPU.SCX;
						case 0xFF44:
							return GB.PPU.LY;
						case 0xFF45:
							return GB.PPU.LYC;
						case 0xFF46:
							GB.Log("Reading from the dma register is not supported");
							return 0x00;
						case 0xFF47:
							return GB.PPU.Palette.GetPaletteByte(PaletteType.Background);
						case 0xFF48:
							return GB.PPU.Palette.GetPaletteByte(PaletteType.Sprite1);
						case 0xFF49:
							return GB.PPU.Palette.GetPaletteByte(PaletteType.Sprite2);
						case 0xFF4A:
							return GB.PPU.WY;
						case 0xFF4B:
							return GB.PPU.WX;
						case 0xFFFF:
							return (byte)GB.CPU.InterruptEnable;
						default:
							if (address >= 0xFF80) return GB.HRam.Read((ushort)(address - 0xFF80));
							return 0xFF;
					}
				}
			}//Check if any memory mapped registers are being accessed, or if the access points to High ram
			GB.Log($"Invalid read to ${address:X}");        //If none of the above worked, this was an invalid read, so we might as well return an invalid value. 
			return 0xFF;
		}
		public void SetByte(ushort address, byte value)
		{
			if (address < 0x8000) { Write(address, value); return; }

			StandartSetByte(address, value);
			return;
		}
		public void StandartSetByte(ushort address,byte value) {
			if (address < 0x8000) {
				GB.Error("Invalid Write");
				return;
			}
			if (address < 0x9800)
			{
				var cram = GB.CRAMBanks[(address - 0x8000) / 0x800];
				cram.Write((ushort)((address - 0x8000) % 0x800),value);
				return;
			}
			if (address < 0xA000)
			{
				var tmram = GB.TMRAMBanks[(address - 0x9800) / 0x400];
				tmram.Write((ushort)((address - 0x9800) % 0x400),value);
				return;
			}
			if (address < 0xC000) { SRam.Write((ushort)(address - 0xA000 + SRamBankOffset), value); return; }
			if (address < 0xE000) { GB.WRam.Write((ushort)(address - 0xC000), value); return; }
			if (address < 0xFE00) { GB.WRam.Write((ushort)(address - 0xE000), value); return; }
			if (address < 0xFF00) { GB.PPU.OAM.Write((ushort)(address - 0xFE00), value); return; }
			if ((address & 0xFF00) == 0xFF00)
			{
				switch (address)
				{
					case 0xFFFF:
						GB.CPU.InterruptEnable = (InterruptType)(value & 31); return;
					case 0xFF04:
						GB.Timers.ResetDIV(); return;
					case 0xFF05:
						GB.Timers.ResetTimer(); return;
					case 0xFF06:
						GB.Timers.SetTMA(value); return;
					case 0xFF07:
						GB.Timers.SetTAC(value); return;
					case 0xFF0F:
						GB.CPU.InterruptFlags = (InterruptType)(value & 31); return;
					case 0xFF00:
						GB.Joypad.Write((ushort)(address - 0xFF00), value); return;
					case 0xFF40:
						GB.PPU.LCDC = value; return;
					case 0xFF41:
						GB.PPU.STAT = value; return;
					case 0xFF42:
						GB.PPU.SCY = value; return;
					case 0xFF43:
						GB.PPU.SCX = value; return;
					case 0xFF44:
						return;//LY is readonly
					case 0xFF45:
						GB.PPU.LYC = value; return;
					case 0xFF46:
						GB.CPU.StartDMA((ushort)(value << 8), 0xA0, 0xFE00); return;
					case 0xFF47:
						GB.PPU.Palette.SetPaletteByte(PaletteType.Background, value);return;
					case 0xFF48:
						GB.PPU.Palette.SetPaletteByte(PaletteType.Sprite1, value);return;
					case 0xFF49:
						GB.PPU.Palette.SetPaletteByte(PaletteType.Sprite2, value);return;
					case 0xFF4A:
						GB.PPU.WY = value; return;
					case 0xFF4B:
						GB.PPU.WX = value; return;
					default:
						if (address >= 0xFF80) GB.HRam.Write((ushort)(address - 0xFF80), value);
						return;
				}
			}
			GB.Log($"Invalid write to ${address:X} with the value of ${value:X}");
		}
		public virtual byte Read(ushort address)
		{
			return DirectRead(address);
		}
		public virtual byte Read(int address)
		{
			return DirectRead(address);
		}
		public virtual byte DirectRead(ushort address)
		{
			return DirectRead((int)address);
		}
		public virtual byte DirectRead(int address)
		{
			return RomImage[address];
		}
		public virtual void Write(ushort address, byte value)
		{
			DirectWrite(address,value);
		}
		public virtual void DirectWrite(ushort address, byte value)
		{
			DirectWrite((int)address,value);
		}
		public virtual void DirectWrite(int address, byte value)
		{
			//GB.Log($"Write to ROM [{address:X}] = {value:X}");
			return;
			//StandartSetByte((ushort)address,value);
		}

		public static MemoryMapper GetMapperObject(GBInstance gb, byte mapperId,byte[] rom) {
			return mapperId switch
			{
				//ROM
				0 => new MemoryMapper(gb, rom, false),
				1 => new MBC1(gb, rom, false),
				2 => new MBC1(gb, rom, false),
				3 => new MBC1(gb, rom, true),
				//ROM + RAM
				8 => new MemoryMapper(gb, rom, false),
				//ROM + RAM + BATTERY
				9 => new MemoryMapper(gb, rom, true),
				_ => throw new NotImplementedException($"Mapper type {mapperId:X} has not been implemented yet."),
			};
		}
		public static MemoryMapper GetMemoryMapper(GBInstance gb, string romFilename) {
			byte[] rom = File.ReadAllBytes(romFilename);    //Read the rom from the given file
			if (rom.Length < 0x8000)    //Measure the size of the rom file, and pad it with $00 bytes if necessary
			{
				gb.Log("Rom file is smaller than expected: Padding rom with 0x00 bytes. Real Gameboy wouldn't care.");
				byte[] rom1 = new byte[0x8000];
				Array.Fill<byte>(rom1, 0);
				rom.CopyTo(rom1.AsSpan());
			}
			byte mapperId = rom[0x0147];    //Get the type of the rom mapper
			var mapper = GetMapperObject(gb, mapperId, rom);    //Initialize the rom mapper.
			//The Gameboy and most emulators perform a series of checks at this step to make sure that the cartridge header is correct, and that the rom is not corrupt.
			//These checks are completely optional, and most emulators which do them can boot the rom regardles of the results. I decided to skip these checks completely.
			//Init console's vram
			ushort address = 0x8000;    //Load the Nintendo Logo's tiles into vram
			foreach (byte b in BOOT_LOGO_CHAR)
			{
				mapper.SetByte(address++,b);
			}
			address = 0x9904;           //Draw the first row of tiles of the logo
			foreach (byte b in BOOT_LOGO_LINE_1)
			{
				mapper.SetByte(address++, b);
			}
			address = 0x9924;           //Draw the second row of tiles of the logo
			foreach (byte b in BOOT_LOGO_LINE_2)
			{
				mapper.SetByte(address++, b);
			}
			return mapper;
		}

		public int IndexOf(byte item)
		{
			throw new NotImplementedException();
		}
		public void Insert(int index, byte item)
		{
			SetByte((ushort)index, item);
		}
		public void RemoveAt(int index)
		{
			SetByte((ushort)index, 0);
		}
		public void Add(byte item)
		{
			throw new NotImplementedException();
		}
		public void Clear()
		{
			throw new NotImplementedException("Cannot clear a memory map.");
		}
		public bool Contains(byte item)
		{
			foreach (byte b in this) {
				if (b == item) return true;
			}
			return false;
		}
		public void CopyTo(byte[] array, int arrayIndex)
		{
			for (int i = 0; i <= ushort.MaxValue; i++) {
				array[arrayIndex + i] = GetByte((ushort)i);
			}
		}
		public bool Remove(byte item)
		{
			throw new NotImplementedException("Cannot remove bytes from ROM");
		}
		public IEnumerator<byte> GetEnumerator()
		{
			for (int i = 0; i <= ushort.MaxValue; i++) {
				yield return GetByte((ushort)i);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		public static explicit operator Stream(MemoryMapper mapper) {
			return new MemoryMappedStream(mapper);
		}

		static readonly byte[] BOOT_LOGO_CHAR = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x0F, 0x00, 0x3F, 0x00, 0x3F, 0x00, 0x0F, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0xC0, 0x00, 0x0F, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0xC0, 0x00, 0x03, 0x00, 0x03, 0x00, 0x03, 0x00, 0x03, 0x00, 0x03, 0x00, 0x03, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xC0, 0x00, 0xC0, 0x00, 0xC0, 0x00, 0xC0, 0x00, 0xC0, 0x00, 0xC0, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x3F, 0x00, 0x3F, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x0F, 0x00, 0x0F, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF3, 0x00, 0xF0, 0x00, 0xF0, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xCF, 0x00, 0xC3, 0x00, 0xC3, 0x00, 0x0F, 0x00, 0x0F, 0x00, 0x0F, 0x00, 0x0F, 0x00, 0x0F, 0x00, 0x0F, 0x00, 0xFC, 0x00, 0xFC, 0x00, 0x3C, 0x00, 0x42, 0x00, 0xB9, 0x00, 0xA5, 0x00, 0xB9, 0x00, 0xA5, 0x00, 0x42, 0x00, 0x3C, 0x00 };
		static readonly byte[] BOOT_LOGO_LINE_1 = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x19 };
		static readonly byte[] BOOT_LOGO_LINE_2 = { 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18 };

		public void initRegisters() {
			//This method initializes the emulator's registers to those of the Gameboy's DMG-C chip (These values might be different on different hardware revisions)
			//Store a reference to the CPU and the PPU in a local variable so I don't have to retype the whole name over and over.
			//This does not affect the performance in any way, as the compiler would do this under the hood anyway.
			var cpu = GB.CPU;   
			var ppu = GB.PPU;

			//Check if the ROM's checksum is equal to 0. Some of the CPU's flags differ depending on the results of this check.
			var checksum = GetByte(0x014D,true)!=0;

			//Setup the CPU's registers
			cpu.A = 0x01;
			cpu.B = 0x00;
			cpu.C = 0x13;
			cpu.D = 0x00;
			cpu.E = 0xD8;
			cpu.H = 0x01;
			cpu.L = 0x4D;

			//Setup the CPU's flags
			cpu.Zero = true;
			cpu.SubFlag = false;
			cpu.Carry = checksum;
			cpu.HalfCarry = checksum;

			//Setup the Gameboy's timers
			GB.Timers.Div = 0xAC;
			GB.Timers.Timer = 0;
			GB.Timers.TMA = 0;
			GB.Timers.InternalSetTAC(0xF8); //This is just a setter method.

			//Setup the Gameboy's PPU and it registers
			ppu.LCDC = 0x91;
			ppu.STAT = 0x85;
			ppu.SCY = 0;
			ppu.SCX = 0;
			ppu.LYC = 0;
			ppu.WX = 0;
			ppu.WY = 0;

			//Setup the colour palettes. These methods are also just setter methods
			ppu.Palette.SetPaletteByte(PaletteType.Background, 0xFC);
			ppu.Palette.SetPaletteByte(PaletteType.Sprite1, 0);
			ppu.Palette.SetPaletteByte(PaletteType.Sprite2, 0);

			//Setup the Gameboy's controller input
			GB.Joypad.DirectWrite(0,0xCF);
			GB.Joypad.SetButton(GameboyInput.Right,false);
			GB.Joypad.SetButton(GameboyInput.Left,false);
			GB.Joypad.SetButton(GameboyInput.Up,false);
			GB.Joypad.SetButton(GameboyInput.Down,false);
			GB.Joypad.SetButton(GameboyInput.A,false);
			GB.Joypad.SetButton(GameboyInput.B,false);
			GB.Joypad.SetButton(GameboyInput.Select,false);
			GB.Joypad.SetButton(GameboyInput.Start,false);
			GB.Joypad.JoypadBankHigher = true;

			//Setup the CPU's interrupts
			cpu.InterruptEnable = 0;
			cpu.InterruptFlags = InterruptType.VBlank;
		}
	}
}
