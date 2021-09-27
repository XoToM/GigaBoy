using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public class VRAM : MMIODevice
    {
        public GBInstance GB { get; init; }
        public byte[] Memory = new byte[0x2000];

        public VRAM(GBInstance gb) {
            GB = gb;
        }
        public byte Read(ushort address)
        {
            if (GB.PPU.State == PPUStatus.GeneratePict) return 0xFF;
            return DirectRead(address);
        }
        public byte DirectRead(ushort address)
        {
            return Memory[address];
        }

        public void Write(ushort address, byte value)
        {
            throw new NotImplementedException();
        }
    }
}
