using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public class RAM : MMIODevice
    {
        public GBInstance GB { get; init; }
        public byte[] Memory { get; init; }
        public bool Reading { get; set; } = true;
        public bool Writing { get; set; } = true;
        public byte DisabledReadData { get; set; } = 0xFF;

        public RAM(GBInstance gb,ushort capacity) {
            GB = gb;
            Memory = new byte[capacity];
        }
        public byte Read(ushort address)
        {
            if (GB.PPU.State == PPUStatus.GeneratePict) return 0xFF;
            return DirectRead(address);
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
