using GigaBoy.Components.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Mappers
{
    public class MemoryMapper: MMIODevice
    {
        public byte[] RomImage { get; set; }
        public GBInstance GB { get; protected set; }
        public RAM SRam { get; protected set; }
        public MemoryMapper(GBInstance gb,byte[] romImage) {
            RomImage = romImage;
            GB = gb;
            SRam = new(gb, 0x2000) {Type = RAMType.SRAM };
        }
        public byte GetByte(ushort address)
        {
            GB.Log($"Read [{address:X}]");
            if (address < 0x8000) return Read(address);

            if (address < 0xA000) return GB.VRam.Read((ushort)(address - 0x8000));
            if (address < 0xC000) return SRam.Read((ushort)(address - 0xA000));
            if (address < 0xE000) return GB.WRam.Read((ushort)(address - 0xC000));
            if (address < 0xFE00) return GB.WRam.Read((ushort)(address - 0xE000));
            if (address < 0xFF00) return 0xFF;//    OAM and an unused area use these addresses. OAM hasn't been implemented yet, and is currently unusable.
            if (address == 0xFFFF) return (byte)GB.CPU.InterruptEnable;
            if (address >= 0xFF80) return GB.HRam.Read((ushort)(address - 0xFF80));

            switch (address)
            {
                case 0xFF0F:
                    return (byte)GB.CPU.InterruptFlags;
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
                case 0xFF4A:
                    return GB.PPU.WY;
                case 0xFF4B:
                    return GB.PPU.WX;
                default:
                    return 0xFF;
            }
        }
        public void SetByte(ushort address,byte value)
        {
            if (address < 0x8000) Write(address,value);

            if (address < 0xA000) GB.VRam.Write((ushort)(address - 0x8000),value);
            if (address < 0xC000) SRam.Write((ushort)(address - 0xA000),value);
            if (address < 0xE000) GB.WRam.Write((ushort)(address - 0xC000),value);
            if (address < 0xFE00) GB.WRam.Write((ushort)(address - 0xE000),value);
            if (address < 0xFF00) return;//    OAM and an unused area use these addresses. OAM hasn't been implemented yet, and is currently unusable.
            if (address == 0xFFFF) GB.CPU.InterruptEnable = (InterruptType)(value&0x1F);
            if (address >= 0xFF80) GB.HRam.Write((ushort)(address - 0xFF80),value);

            switch (address)
            {
                case 0xFF0F:
                    GB.CPU.InterruptFlags = (InterruptType)(value&0x1F);
                    break;
                case 0xFF40:
                    GB.PPU.LCDC = value;
                    break;
                case 0xFF41:
                    GB.PPU.STAT = value;
                    break;
                case 0xFF42:
                    GB.PPU.SCY = value;
                    break;
                case 0xFF43:
                    GB.PPU.SCX = value;
                    break;
                case 0xFF44:
                    break;
                case 0xFF45:
                    GB.PPU.LYC = value;
                    break;
                case 0xFF4A:
                    GB.PPU.WY = value;
                    break;
                case 0xFF4B:
                    GB.PPU.WX = value;
                    break;
                default:
                    return;
            }
        }
        public byte Read(ushort address)
        {
            return DirectRead(address);
        }
        public byte DirectRead(ushort address)
        {
            if (address > 0x8000) return 0xFF;
            return RomImage[address];
        }
        public void Write(ushort address, byte value)
        {
            DirectWrite(address,value);
        }
        public void DirectWrite(ushort address, byte value)
        {
            return;
        }

        public static MemoryMapper GetMemoryMapper(GBInstance gb,string romFilename) {
            byte[] rom = File.ReadAllBytes(romFilename);
            if (rom.Length < 32768) {
                gb.Log("Rom file is smaller than expected: Padding rom with 0x00 bytes");
                byte[] rom1 = new byte[32768];
                Array.Fill<byte>(rom1,0);
                rom.CopyTo(rom1.AsSpan());
            }
            byte mapper = rom[0x0147];
            switch (mapper) {
                case 0:
                    return new MemoryMapper(gb,rom);
                default:
                    throw new NotImplementedException($"Mapper type {mapper:X} has not been implemented yet.");
            }
        }
    }
}
