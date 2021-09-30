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
        /// <summary>
        /// Calculates and returns the color of the given pixel. 
        /// Only renders the background and the window.
        /// </summary>
        /// <param name="x">X coord of the pixel</param>
        /// <param name="y">Y coord of the pixel</param>
        /// <param name="doScrolling">Should the method calculate everything in global coordinates (false) or screenspace coordinates (true).</param>
        /// <param name="drawWindow">Should the method draw the window (true) or ignore it (false). 
        /// When set to true the window will still be ignored if the pixel is outside of it, or the window is disabled by the ppu.</param>
        /// <returns></returns>
        public Color FetchPixel(byte x,byte y,bool drawWindow,bool doScrolling) {
            ushort tileAddress = 0x9800;
            if (drawWindow && PPU.LCDC.HasFlag(LCDCFlags.WindowEnable) && ((doScrolling && (x >= PPU.WX) && (y >= PPU.WY)) || (!doScrolling && (x+PPU.SCX >= PPU.WX) && (y + PPU.SCY >= PPU.WY))))
            {
                if (PPU.LCDC.HasFlag(LCDCFlags.WindowTileMap)) tileAddress = 0x9C00;
                x = (byte)(x - PPU.WX);
                y = (byte)(y - PPU.WY);
            }
            else {
                if (PPU.LCDC.HasFlag(LCDCFlags.BGTileMap)) tileAddress = 0x9C00;
                if (doScrolling)
                {
                    x = (byte)(x + PPU.SCX);
                    y = (byte)(y + PPU.SCY);
                }
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
