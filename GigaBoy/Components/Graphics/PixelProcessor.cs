using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public class PixelProcessor
    {
        public GBInstance GB { get; init; }
        public PPU PPU { get; init; }
        public Queue<Color> PixelQueue = new Queue<Color>(16);

        public PixelProcessor(PPU ppu) { 
            GB = ppu.GB;
            PPU = ppu;
        }

        public Color FetchPixel(byte x,byte y) {
            if (!PPU.LCDC.HasFlag(LCDCFlags.PPUEnable)) return Color.White;
            ushort tileAddress = 0x9800;
            if (PPU.LCDC.HasFlag(LCDCFlags.WindowEnable) && (x >= PPU.WX) && (y >= PPU.WY))
            {
                if (PPU.LCDC.HasFlag(LCDCFlags.WindowTileMap)) tileAddress = 0x9C00;
                x = (byte)(x - PPU.WX);
                y = (byte)(y - PPU.WY);
            }
            else {
                if (PPU.LCDC.HasFlag(LCDCFlags.BGTileMap)) tileAddress = 0x9C00;
                x = (byte)(x + PPU.SCX);
                y = (byte)(y + PPU.SCY);
            }
            tileAddress += (ushort)((x >> 3) + (y>>3)*32);
            byte tileId = GB.VRam.DirectRead(tileAddress);
            return FetchTilePixel(tileId,(byte)(x&0x07),(byte)(y&0x07),PaletteType.Background);
        }
        public Color FetchTilePixel(byte tileId, byte ox, byte oy, PaletteType paletteType) {
            ushort tileAddress = 0x8000;
            byte color;

            if (tileId > 127)
            {
                tileAddress = 0x8800;
            }
            else if ((paletteType !=PaletteType.Background) && PPU.LCDC.HasFlag(LCDCFlags.TileData)) tileAddress = 0x9000;
            tileAddress += (ushort)(tileId * 16);
            tileAddress += (ushort)(oy * 2);
            byte data1 = GB.VRam.DirectRead(tileAddress);
            byte data2 = GB.VRam.DirectRead(++tileAddress);
            byte mask = (byte)(0b10000000>>ox);
            ox = (byte)(7 - ox);
            data1 = (byte)((data1 & mask) >> ox);
            data2 = (byte)((data2 & mask) >> (--ox));
            color = (byte)(data1 | data2);
            return PPU.Palette.GetTrueColor(color,paletteType);
        }

    }
}
