using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Mappers
{
    public class MBC1 : MemoryMapper
    {
        public int RomXBank { get; set; } = 1;
        public int ExpectedRomSize;
        public byte[,] Banks { get; protected set; }  
        public int BankCount { get; protected set; }
#pragma warning disable CS8618 
        public MBC1(GBInstance gb,byte[] romImage,bool battery) : base(gb,romImage,battery) {   //  Visual Studio complains about the Banks property not being set, but it does get set in the SplitIntoBanks method which is called at the end of the constructor. Visual Studio just doesn't detect this.
#pragma warning restore CS8618 
            byte romSizeByte = romImage[0x148];
            var romSize = romSizeByte switch
            {
                //Likely incorrect, but I included it just in case its used by someone somewhere.
                0x54 => 1572864,
                //Likely incorrect, but I included it just in case its used by someone somewhere.
                0x52 => 1153434,
                //Likely incorrect, but I included it just in case its used by someone somewhere.
                0x53 => 1258292,
                _ => 0x8000 << romSizeByte
            };
            if (RomImage.Length < romSize)
            {
                gb.Log("Rom file is smaller than expected: Padding rom with 0x00 bytes. Real gameboy wouldn't care.");
                byte[] rom1 = new byte[romSize];
                Array.Fill<byte>(rom1, 0);
                RomImage.CopyTo(rom1.AsSpan());
                RomImage = rom1;
            }
            ExpectedRomSize = romSize;
            SplitIntoBanks();
        }

        public override void DirectWrite(int address,byte value) {
            if (address < 0) return;
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
            if (address <= 0x4000) {
                value = (byte)(value & 0x1F);
                if (value == 0) value = 1;
                RomXBank = value % ((ExpectedRomSize / 0x4000) - 1);
                return;
            }
            throw new NotImplementedException("Ram banking, Large Rom Banking, and mode switching has not been implemented yet.");
            //if (address < 0x6000) {
                //RomBankOffset = (RomBankOffset & 0xFFFFF) | 
                //RomBankOffset = RomBankOffset % RomImage.Length;
                //return;
            //}
        }
        public override byte DirectRead(int address)
        {
            if (address < 0) return 0;
            if (address < 0x4000) {
                return Banks[0,address];
            }
            if (address < 0x8000) {
                return Banks[RomXBank,address - 0x4000];
            }
            if (address < 0xA000) {
                return 0;
            }
            if (address < 0xC000) {
                throw new NotImplementedException("Cartridge RAM not implemented yet");
            }
            return 0;
        }
        public virtual void SplitIntoBanks() {
            BankCount = ExpectedRomSize / 0x4000;
            Banks = new byte[BankCount,0x4000];
            for (int i = 0; i < BankCount; i++) {
                var window = RomImage.AsSpan(i*0x4000, 0x4000);
                for (int j = 0; j < 0x4000;j++) {
                    Banks[i, j] = window[j];
                }
            }
        }
    }
}
