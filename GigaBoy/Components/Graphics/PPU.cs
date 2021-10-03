using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public enum PPUStatus : sbyte {HBlank,VBlank,OAMSearch,GenerateFrame}
    public class PPU
    {
        public GBInstance GB { get; init; }
        public ColorPalette Palette { get; init; }
        public FIFO FIFO { get; init; }

        public IEnumerator<PPUStatus> Renderer { get; protected set; }

        protected ColorContainer[] frameBuffer = new ColorContainer[160 * 144];
        protected ColorContainer[] displayBuffer = new ColorContainer[160 * 144];
        public ColorContainer clearColor { get; set; }

        #region LcdcStat
        public bool Enabled { get; set; } = false;
        public bool WindowEnable { get; set; } = false;
        public bool BackgroundTileMap { get; set; } = false;
        public bool WindowTileMap { get; set; } = false;
        public bool TileData { get; set; } = false;
        public PPUStatus State { get; protected set; } = PPUStatus.VBlank;

        #endregion

        #region Registers
        public byte SCX { get; set; } = 0;
        public byte SCY { get; set; } = 0;
        public byte WX { get; set; } = 0;
        public byte WY { get; set; } = 0;
        public byte LY { get; protected set; } = 0;
        #endregion

        public PPU(GBInstance gb) {
            GB = gb;
            Palette = new();
            Renderer = Scanner().GetEnumerator();
            FIFO = new(this);
        }
        #region ScanlineRendering
        public Span2D<ColorContainer> GetFrame() {
            return new Span2D<ColorContainer>(frameBuffer,160,144);
        }
        public void FrameDone()
        {
            var dbuffer = displayBuffer;
            displayBuffer = frameBuffer;
            frameBuffer = dbuffer;
            Array.Fill(frameBuffer, clearColor);
        }
        public void Tick() {
            Renderer.MoveNext();
            State = Renderer.Current;
        }
        public IEnumerable<PPUStatus> Scanner() {
            while (true)
            {
                Array.Fill(frameBuffer, clearColor);
                while (!Enabled) {
                    yield return State;
                }
                LY = 0;
                while (Enabled) {
                    IEnumerator<(byte, byte)?>? pixelFetcher;
                    if (LY < 144)
                    {
                        FIFO.Reset();
                        int delayDots = 0;
                        int XCoord = 0;
                        //Search OAM Here
                        for (int i = 0; i < 80; i++)
                        {
                            ++delayDots;
                            yield return PPUStatus.OAMSearch;
                        }
                        pixelFetcher = FIFO.FetchTileData(false).GetEnumerator();
                        //pixelFetcher.
                        while (FIFO.backgroundQueue.Count <= 8)
                        {
                            while (!pixelFetcher.Current.HasValue)
                            {
                                pixelFetcher.MoveNext();
                                ++delayDots;
                                yield return PPUStatus.GenerateFrame;
                            }
                            FIFO.EnqueuePixels(pixelFetcher.Current.Value);
                        }
                        //Enqueue pixels for sprites if needed here.

                        for (int i = 0; i < (SCX & 3); i++) {
                            ++delayDots;
                            FIFO.ShiftOut();
                            yield return PPUStatus.GenerateFrame;
                        }
                        while(XCoord<160)
                        {
                            ++delayDots;
                            yield return PPUStatus.GenerateFrame;
                        }
                        for (int i = 0; i < 456 - delayDots; i++)
                        {
                            yield return PPUStatus.HBlank;
                        }
                    }
                    if (LY > 143) {
                        for (int i=0;i<456;i++) {
                            yield return PPUStatus.VBlank;
                        }
                    }
                    ++LY;
                    if (LY >= 154) LY = 0;
                }
            }
        } 
        #endregion
        #region BlockRendering
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
#endregion
        #region FormatConverters
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
        #endregion
    }
}
