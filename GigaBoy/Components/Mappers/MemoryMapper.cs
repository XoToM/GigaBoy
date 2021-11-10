using GigaBoy.Components.Graphics;
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
        public int RomBankOffset { get; set; } = 0;

        public int Count => ushort.MaxValue + 1;

        public bool IsReadOnly => true;

        public byte this[int index] { get => GetByte((ushort)index); set => SetByte((ushort)index, value); }

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
            if (address < 0x9800)
            {
                var cram = GB.CRAMBanks[(address - 0x8000) / 0x800];
                return direct ? cram.DirectRead((ushort)((address - 0x8000) % 0x800)) : cram.Read((ushort)((address - 0x8000) % 0x800));
            }
            if (address < 0xA000)
            {
                var tmram = GB.TMRAMBanks[(address - 0x9800) / 0x400];
                return direct ? tmram.DirectRead((ushort)((address - 0x9800) % 0x400)) : tmram.Read((ushort)((address - 0x9800) % 0x400));
            }
            if (address < 0xC000) return SRam.Read((ushort)(address - 0xA000 + SRamBankOffset));
            if (address < 0xE000) return GB.WRam.Read((ushort)(address - 0xC000));
            if (address < 0xFE00) return GB.WRam.Read((ushort)(address - 0xE000));
            if (address < 0xFF00) return 0xFF;//    OAM and an unused area use these addresses. OAM hasn't been implemented yet, and is currently unusable.
            if ((address & 0xFF00) == 0xFF00)
            {
                switch (address)
                {
                    case 0xFFFF:
                        return (byte)GB.CPU.InterruptEnable;
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
                        if (address >= 0xFF80) return GB.HRam.Read((ushort)(address - 0xFF80));
                        return 0xFF;
                }
            }
            return 0;
        }
        public void SetByte(ushort address, byte value)
        {
            if (address < 0x8000) { Write(address, value); return; }

            StandartSetByte(address, value);
            return;
        }
        public void StandartSetByte(ushort address,byte value) {
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
            if (address < 0xFF00) return;//    OAM and an unused area use these addresses. OAM hasn't been implemented yet, and is currently unusable.
            if ((address & 0xFF00) == 0xFF00)
            {
                switch (address)
                {
                    case 0xFFFF:
                        GB.CPU.InterruptEnable = (InterruptType)(value & 31); return;
                    case 0xFF0F:
                        GB.CPU.InterruptFlags = (InterruptType)(value & 31); return;
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
                    case 0xFF4A:
                        GB.PPU.WY = value; return;
                    case 0xFF4B:
                        GB.PPU.WX = value; return;
                    default:
                        if (address >= 0xFF80) GB.HRam.Write((ushort)(address - 0xFF80), value);
                        return;
                }
            }
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
            return RomImage[address];
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
            return;
        }

        public static MemoryMapper GetMapperObject(GBInstance gb,byte mapperId,byte[] rom) {
            switch (mapperId)
            {
                case 0:     //ROM
                    return new MemoryMapper(gb, rom, false);
                case 1:
                    return new MBC1(gb, rom, false);
                case 2:
                    return new MBC1(gb, rom, false);
                case 3:
                    return new MBC1(gb, rom, true);
                case 8:     //ROM + RAM
                    return new MemoryMapper(gb, rom, false);
                case 9:     //ROM + RAM + BATTERY
                    return new MemoryMapper(gb, rom, true);
                default:
                    throw new NotImplementedException($"Mapper type {mapperId:X} has not been implemented yet.");
            }
        }
        public static MemoryMapper GetMemoryMapper(GBInstance gb,string romFilename) {
            byte[] rom = File.ReadAllBytes(romFilename);
            if (rom.Length < 0x8000)
            {
                gb.Log("Rom file is smaller than expected: Padding rom with 0x00 bytes. Real gameboy wouldn't care.");
                byte[] rom1 = new byte[0x8000];
                Array.Fill<byte>(rom1, 0);
                rom.CopyTo(rom1.AsSpan());
            }
            byte mapperId = rom[0x0147];
            //Check the header checksum here.
            var mapper = GetMapperObject(gb, mapperId, rom);
            //Check the header here.
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
    }
}
