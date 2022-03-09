using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GigaBoy.Components.Graphics;

namespace GigaBoy.Components
{
    public enum RAMType {RAM,SRAM,HRAM,OAM }
    public class RAM : MMIODevice
    {
        public GBInstance GB { get; init; }
        public byte[] Memory { get; init; }
        public bool Reading { get; set; } = true;
        public bool Writing { get; set; } = true;
        public int Size { get { return Memory.Length; } }
        public byte DisabledReadData { get; set; } = 0xFF;
        public RAMType Type { get; init; } = RAMType.RAM;

        public RAM(GBInstance gb,int capacity) {
            GB = gb;
            Memory = new byte[capacity];
        }
        public virtual bool Available()
        {
            return true;
            /*              //Emulator's timings are currently incorrect. I prioritize compatibiity over accuracy in this case, so it currently always returns true.
            return Type switch
            {
                RAMType.SRAM or RAMType.RAM or RAMType.HRAM => true,
                RAMType.OAM => GB.PPU.Enabled && (GB.PPU.State == PPUStatus.VBlank || GB.PPU.State == PPUStatus.HBlank),
                _ => false,
            };*/
        }

        public virtual byte Read(ushort address)
        {
            if(Available() && Reading) return DirectRead(address);
            return DisabledReadData;
        }
        public virtual byte Read(int address)
        {
            return Read((ushort)address);
        }
        public virtual byte DirectRead(ushort address)
        {
            if (address >= Size)
                GB.Error($"Invalid Read [{address:X}]");
            return Memory[address];
        }
        public virtual byte DirectRead(int address) {
            return DirectRead((ushort)address);
        }
        public virtual Span<byte> DirectRead(ushort address,ushort count)
        {
            int addr = address;
            if (addr + count >= Size)
            {
                GB.Error($"Invalid Block Read (from {address:X} to {(addr + count):X})");
            }
            return Memory.AsSpan(addr,count);
        }

        public virtual void Write(ushort address, byte value)
        {
            if (Available() && Writing)
            {
                DirectWrite(address, value);
            }
            else {
                GB.Log($"Writing to Ram its disabled [{address:X}] = {value:X}");
            }
        }
        public virtual void Write(int address, byte value) {
            Write((ushort)address, value);
        }
        public virtual void DirectWrite(ushort address, byte value)
        {
            if (address >= Size)
                GB.Error($"Invalid Write [{address:X}] = {value:X}");
            Memory[address] = value;
        }
        public virtual void DirectWrite(int address, byte value)
        {
            DirectWrite((ushort)address, value);
        }
    }
}
