using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
        protected Color GetPixel(byte x,byte y,bool drawWindow=true,bool doScrolling=true) {
            //PPU is disabled, so we return instantly.
            if (!LCDC.HasFlag(LCDCFlags.PPUEnable)) return Color.White;
            return PixelProcessor.FetchPixel(x,y,drawWindow,doScrolling);
        }
        public void ForceDrawTile(ushort address,Bitmap image,int x,int y,PaletteType palette) {
            int bx = x;
            for (int v = 0; v < 8; v++) {
                byte data1 = GB.VRam.DirectRead(address++);
                byte data2 = GB.VRam.DirectRead(address++);
                byte shift = 7;
                
                for (int u = 0; u < 8; u++)
                {
                    var color = ((data2 >> shift)<<1)&2;
                    color = ((data1 >> shift)&1) | color;
                    --shift;
                    image.SetPixel(x+u,y+v,Palette.GetTrueColor((byte)color,palette));
                }
            }
        }
        public void GetTileMapBlock(int x,int y, Span2D<byte> tiles,ushort tileMapAddr) {
            int address = tileMapAddr;
            var vram = GB.VRam;
            y = y * 32;
            for (int v = 0; v < tiles.Height; v++) {
                for (int u = 0; u < tiles.Width; u++) {
                    ushort addr = (ushort)(address + u + x + y + v * 32);
                    var data = vram.DirectRead(addr);
                    tiles[v, u] = data;
                }
            }
        }
        public void DrawTileMap(Span2D<ColorContainer> image,int x, int y, int width, int height, ushort tileMapAddr, ushort tileDataAddr, PaletteType palette) {
            if (width+x > 32 || height+y > 32 || x > 0 || y > 0) throw new IndexOutOfRangeException("Position is out of bounds");
            Span2D<byte> tileMap = new((stackalloc byte[width*height]),width,height);
            GetTileMapBlock(x, y, tileMap, tileMapAddr);
            DrawRegion(tileMap,image,tileDataAddr,palette);
        }
        public Span<byte> GetTileData(byte tile,ushort tileDataAddr) {
            int address = tileDataAddr;
            if (tile > 127) address = 0x8000;
            return GB.VRam.DirectRead((ushort)(address+tile*16),16);
        }
        public void DrawRegion(Span2D<byte> tilemap,Span2D<ColorContainer> image,ushort tileDataAddr,PaletteType palette) {
            if (image.Width < tilemap.Width * 8 || image.Height < tilemap.Height * 8) throw new OutOfMemoryException($"Image buffer needs to be at least {tilemap.Width * 8}x{tilemap.Height * 8}");
            Span<ColorContainer> colors = stackalloc ColorContainer[256];
            for (int y = 0; y < tilemap.Height; y++) {
                for (int x = 0; x < tilemap.Width; x++) {
                    var td = GetTileData(tilemap[y,x],tileDataAddr);
                    Palette.ToColors(td, colors, palette);
                    for (int v = 0; v < 8; v++)
                    {
                        for (int u = 0; u < 8; u++)
                        {
                            image[y * 8 + v, x * 8 + u] = colors[v*8+u];
                        }
                    }
                }
            }
        }

        public void DrawBitmap(Span2D<ColorContainer> image,Bitmap bitmap,int x,int y) {
            if (bitmap.Width < image.Width + x || bitmap.Height < image.Width + y) throw new ArgumentOutOfRangeException($"Bitmap is too small. It has to be at least {image.Width + x}x{image.Width + y}");
            Span<byte> bytes = stackalloc byte[image.Width*image.Height*4];
            for (int v = 0; v < image.Height; v++) {
                for (int u = 0; u < image.Width; u++) {
                    var p = (v*image.Width+u) * 4;
                    Color c = image[v, u];
                    bytes[p] = c.R;
                    bytes[++p] = c.G;
                    bytes[++p] = c.B;
                    bytes[++p] = c.A;
                }
            }

            var bdata = bitmap.LockBits(new Rectangle(x,y,image.Width,image.Height),System.Drawing.Imaging.ImageLockMode.WriteOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int size = Math.Abs(bdata.Stride) * bitmap.Height;
            Marshal.Copy(bytes.ToArray(),0, bdata.Scan0,size);
            bitmap.UnlockBits(bdata);
        }
    }
}
