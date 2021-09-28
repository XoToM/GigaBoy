using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public enum PPUStatus : sbyte {HBlank,VBlank,OAMSearch,GeneratePict }
    [Flags]
    public enum LCDCFlags : byte {BGPriority=1,OBJEnable=2,OBJSize=4,BGTileMap=8,TileData=16,WindowEnable=32,WindowTileMap=64,PPUEnable=128 }
    public class PPU
    {
        public GBInstance GB { get; init; }
        public ColorPalette Palette { get; init; }
        public PixelProcessor PixelProcessor { get; init; }
        public int Dot { get; protected set; }
        public Bitmap? CurrentImage { get; protected set; }
        public PPUStatus State { get; protected set; }
        /// <summary>
        /// This register stores the current scanline.
        /// </summary>
        public int LY { get; protected set; } = 0;
        public byte SCX { get; set; } = 0;
        public byte SCY { get; set; } = 0;
        public byte WX { get; set; } = 0;
        public byte WY { get; set; } = 0;

        public LCDCFlags LCDC { get; set; }
        //ToDo: Implement the STAT register

        public PPU(GBInstance gb) {
            GB = gb;
            Palette = new();
            PixelProcessor = new(this);
        }
        public Bitmap GetInstantImage() {
            Bitmap result = new(160,144);
            for (byte y = 0; y < 144; y++) {
                for (byte x = 0; x < 160; x++)
                {
                    result.SetPixel(x,y,GetPixel(x,y));
                }
            }
            return result;
        }
        protected Color GetPixel(byte x,byte y) {
            return PixelProcessor.FetchPixel(x,y);
        }
    }
}
