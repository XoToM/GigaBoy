using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Mappers
{
    public class MBC1 : MemoryMapper
    {
        public MBC1(GBInstance gb,byte[] romImage,bool battery) : base(gb,romImage,battery) {
            byte romSizeByte = DirectRead(0x148);
            int romSize;
            switch (romSizeByte) {
                case 0x54://Likely incorrect, but I included it just in case its used by someone somewhere.
                    romSize = 1572864;
                    break;
                case 0x52://Likely incorrect, but I included it just in case its used by someone somewhere.
                    romSize = 1153434;
                    break;
                case 0x53://Likely incorrect, but I included it just in case its used by someone somewhere.
                    romSize = 1258292;
                    break;
                default:
                    romSize = 0x8000 << romSizeByte;
                    break;
            }
            if (RomImage.Length < romSize)
            {
                gb.Log("Rom file is smaller than expected: Padding rom with 0x00 bytes. Real gameboy wouldn't care.");
                byte[] rom1 = new byte[romSize];
                Array.Fill<byte>(rom1, 0);
                RomImage.CopyTo(rom1.AsSpan());
                RomImage = rom1;
            }
        }

        public override void DirectWrite(ushort address,byte value) {
            if (address < 0x2000)
            {
                if ((value & 0x0F) == 0x0A)
                {
                    SRam.Writing = true;
                    SRam.Reading = true;
                }
                else {
                    SRam.Writing = false;
                    SRam.Reading = false;
                    SRam.DisabledReadData = 0xFF;
                    //ToDo: Implement Saving sram.
                    GB.Log("Saving SRAM has not been implemented yet.");
                }
                return;
            }
            if (address < 0x4000) {
                if (value == 0) value = 1;
                RomBankOffset = (RomBankOffset & 0x307FFF) | ((value&0x1F)<<15);
                RomBankOffset = RomBankOffset % RomImage.Length;
                return;
            }
            throw new NotImplementedException("Ram banking, Large Rom Banking, and mode switching has not been implemented yet.");
            //if (address < 0x6000) {
                //RomBankOffset = (RomBankOffset & 0xFFFFF) | 
                //RomBankOffset = RomBankOffset % RomImage.Length;
                //return;
            //}
        }
    }
}
