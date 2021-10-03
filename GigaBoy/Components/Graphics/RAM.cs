﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public enum RAMType {RAM,VRAM,SRAM,HRAM,OAM }
    public class RAM : MMIODevice
    {
        public GBInstance GB { get; init; }
        public byte[] Memory { get; init; }
        public bool Reading { get; set; } = true;
        public bool Writing { get; set; } = true;
        public int Size { get { return Memory.Length; } }
        public byte DisabledReadData { get; set; } = 0xFF;
        public ushort AddressOffset { get; init; } = 0xFF;
        public RAMType Type { get; init; } = RAMType.RAM;

        public RAM(GBInstance gb,ushort capacity) {
            GB = gb;
            Memory = new byte[capacity];
        }
        public byte Read(ushort address)
        {
            if(!Available()) return DisabledReadData;
            return DirectRead(address);
        }
        public bool Available() {
            //If a DMA transfer is running return false, if not continue. HRAM isnt used by DMA.
            //TODO: Uncomment this when DMA is finished.
            //if(Type!=RAMType.HRAM&&DMA.Active)return false;
            switch (Type) {
                case RAMType.RAM:
                case RAMType.SRAM:
                case RAMType.HRAM:
                    return true;
                case RAMType.OAM:
                    return GB.PPU.Enabled&&(GB.PPU.State == PPUStatus.VBlank || GB.PPU.State == PPUStatus.HBlank);
                case RAMType.VRAM:
                    return GB.PPU.State == PPUStatus.GenerateFrame;
                default:
                    return false;
            }
        }
        public byte DirectRead(ushort address)
        {
            if (address >= 0xA000 || address < 0x8000)
                GB.Error($"Invalid Read [{address:X}]");
            return Memory[address - 0x8000];
        }
        public Span<byte> DirectRead(ushort address,ushort count)
        {
            int addr = address;
            if (addr + count >= 0xA000 || address < 0x8000)
            {
                GB.Error($"Invalid Block Read (from {address:X} to {(addr + count):X})");
            }
            addr -= 0x8000;
            return Memory.AsSpan(addr,count);
        }

        public void Write(ushort address, byte value)
        {
            if (!Available()) return;
            throw new NotImplementedException();
        }
        public void DirectWrite(ushort address, byte value)
        {
            if (address >= 0xA000 || address < 0x8000)
                GB.Error($"Invalid Write [{address:X}] = {value:X}");
            Memory[address-0x8000] = value;
        }
    }
}