﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public enum PPUStatus : sbyte {HBlank,VBlank,OAMSearch,GenerateFrame}
    public class PPU : ClockBoundDevice
    {
        public GBInstance GB { get; init; }
        public ColorPalette Palette { get; init; }
        public FIFO FIFO { get; init; }

        public IEnumerator<PPUStatus> Renderer { get; protected set; }

        protected ColorContainer[] frameBuffer = new ColorContainer[160 * 144];
        protected ColorContainer[] displayBuffer = new ColorContainer[160 * 144];
        public ColorContainer clearColor { get; set; }

        #region LcdcStat
        /// <summary>
        /// Bit 7 of LCDC
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Bit 6 of LCDC
        /// </summary>
        public bool WindowTileMap { get; set; } = false;
        /// <summary>
        /// Bit 5 of LCDC
        /// </summary>
        public bool WindowEnable { get; set; } = false;
        /// <summary>
        /// Bit 4 of LCDC
        /// </summary>
        public bool TileData { get; set; } = false;
        /// <summary>
        /// Bit 3 of LCDC
        /// </summary>
        public bool BackgroundTileMap { get; set; } = false;
        /// <summary>
        /// Bit 2 of LCDC
        /// false = 8x8 sprites
        /// true = 8x16 sprites
        /// </summary>
        public bool ObjectSize { get; set; } = false;
        /// <summary>
        /// Bit 1 of LCDC
        /// false = sprites disabled
        /// true = sprites enabled
        /// </summary>
        public bool ObjectEnable { get; set; } = false;
        /// <summary>
        /// Bit 0 of LCDC
        /// false = Background and Window are not displayed
        /// true = Background and Window are displayed if their correspoding enable bits are set
        /// </summary>
        public bool BGWindowPriority { get; set; } = true;
        /// <summary>
        /// Bit 0 and 1 of STAT
        /// </summary>
        public PPUStatus State { get; protected set; } = PPUStatus.VBlank;
        private bool _mode0InterruptEnable;
        public bool Mode0InterruptEnable
        {
            get { return _mode0InterruptEnable; }
            set
            {
                if (value && !Mode0InterruptEnable && (State == PPUStatus.HBlank)) GB.CPU.SetInterrupt(InterruptType.Stat);
                _mode0InterruptEnable = value;
            }
        }
        private bool _mode1InterruptEnable;
        public bool Mode1InterruptEnable
        {
            get { return _mode1InterruptEnable; }
            set
            {
                if (value && !Mode1InterruptEnable && (State == PPUStatus.VBlank)) GB.CPU.SetInterrupt(InterruptType.Stat);
                _mode1InterruptEnable = value;
            }
        }
        private bool _mode2InterruptEnable;
        public bool Mode2InterruptEnable
        {
            get { return _mode2InterruptEnable; }
            set
            {
                if (value && !Mode2InterruptEnable && (State == PPUStatus.OAMSearch)) GB.CPU.SetInterrupt(InterruptType.Stat);
                _mode2InterruptEnable = value;
            }
        }
        private bool _lycInterruptEnable;
        public bool LYCInterruptEnable
        {
            get { return _lycInterruptEnable; }
            set
            {
                if (value && !LYCInterruptEnable && (LY==LYC)) GB.CPU.SetInterrupt(InterruptType.Stat);
                _lycInterruptEnable = value;
            }
        }

        public byte LCDC { get { 
                return (byte)((BGWindowPriority?1:0)|(ObjectEnable?2:0)|(ObjectSize?4:0)|(BackgroundTileMap?8:0)|(TileData?16:0)|(WindowEnable?32:0)|(WindowTileMap?64:0)|(Enabled?128:0));
            }
            set {
                BGWindowPriority = (value & 1) != 0;
                ObjectEnable = (value & 2) != 0;
                ObjectSize = (value & 4) != 0;
                BackgroundTileMap = (value & 8) != 0;
                TileData = (value & 16) != 0;
                WindowEnable = (value & 32) != 0;
                WindowTileMap = (value & 64) != 0;
                Enabled = (value & 128) != 0;
            }
        }
        //  All Gameboy Models before CGB had a hardware bug which caused the stat to be set to 0xFF for one cycle, which could trigger the STAT Interrupt. This bug should be emulated, as some games relied on it to work properly.
        private byte _setStat = 0xFF;
        public byte STAT 
        { 
            get {
                return DirectSTAT;
            }
            set
            {
                _setStat = value;
                DirectSTAT = 0xFF;
            }
        }
        public byte DirectSTAT
        {
            get
            {
                return (byte)((((int)State) & 3) | (LY == LYC ? 4 : 0) | (Mode0InterruptEnable?8:0) | (Mode1InterruptEnable?16:0)|(Mode2InterruptEnable?32:0)|(LYCInterruptEnable?64:0));
            }
            set {
                Mode0InterruptEnable = (value & 8) != 0;
                Mode1InterruptEnable = (value & 16) != 0;
                Mode2InterruptEnable = (value & 32) != 0;
                LYCInterruptEnable = (value & 64) != 0;
            }
        }

        #endregion

        #region Registers
        public byte SCX { get; set; } = 0;
        public byte SCY { get; set; } = 0;
        public byte WX { get; set; } = 0;
        public byte WY { get; set; } = 0;
        private byte _ly = 0;
        public byte LY
        {
            get { return _ly; }
            set { _ly = value; if (_ly == LYC && LYCInterruptEnable) GB.CPU.SetInterrupt(InterruptType.Stat); }
        }

        public byte LYC { get; set; } = 0;
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
            lock (this) {
                var dbuffer = displayBuffer;
                displayBuffer = frameBuffer;
                frameBuffer = dbuffer;
            }
            Array.Fill(frameBuffer, clearColor);
        }
        public void Tick() {
            try
            {
                if (_setStat != 0xFF) {
                    DirectSTAT = _setStat;
                    _setStat = 0xFF;
                }
                Renderer.MoveNext();
                State = Renderer.Current;
            }
            catch (Exception e) {
                GB.Error(e.ToString());
            }
        }
        public IEnumerable<PPUStatus> Scanner() {
            while (true)
            {
                Array.Fill(frameBuffer, clearColor);
                while (!Enabled)
                {
                    yield return State;
                }
                while (Enabled)
                {
                    LY = 0;
                    bool yWindow = false;
                    while (LY < 144)
                    {
                        if (LY == WY && WindowEnable) yWindow = true;
                        foreach (var s in ScanlineScanner(yWindow)) yield return s;
                        //GB.Log($"Scanline = {LY}");
                        if (!WindowEnable) yWindow = false;
                        ++LY;
                    }
                    GB.CPU.SetInterrupt(InterruptType.VBlank);
                    if (Mode1InterruptEnable) GB.CPU.SetInterrupt(InterruptType.Stat);
                    FrameDone();
                    for (int i = 0; i < 10; i++)
                    {
                        //GB.Log($"VBlank = {LY}");
                        yield return PPUStatus.VBlank;
                        ++LY;
                    }
                }
            }
        }
        protected void SetPixel(int x,int y,ColorContainer color) {
            //GB.Log($"Pixel Set at ({x}, {y})");
            var screen = new Span2D<ColorContainer>(frameBuffer, 160, 144);
            screen[y, x] = color;
        }
        public Bitmap GetInstantImage() {
            lock (this)
            {
                try
                {
                    Span2D<ColorContainer> img = new(displayBuffer, 160, 144);
                    var bmp = new Bitmap(160, 144);
                    DrawBitmap(img, bmp, 0, 0);
                    return bmp;
                }
                catch (Exception e)
                {
                    GB.Error(e.ToString());
                    var b = new Bitmap(1, 1);
                    b.SetPixel(0, 0, Color.Pink);
                    return b;
                }
            }
        }
        public IEnumerable<PPUStatus> ScanlineScanner(bool yWindow) {
            int delayDots = 0;
            var stat = PPUStatus.OAMSearch;
            if (Mode2InterruptEnable) GB.CPU.SetInterrupt(InterruptType.Stat);
            for (; delayDots < 80; delayDots++) {
                yield return stat;
            }

            stat = PPUStatus.GenerateFrame;

            int xPixel = 0 - (SCX & 7);

            bool xWindow = false;
            bool usingWindow = false;
            ushort backgroundAddress = calculateBackgroundTileMapAddress();
            ushort windowAddress = calculateWindowTileMapAddress();
            IEnumerator<(byte,byte)?> fetcher = FIFO.FetchTileData(backgroundAddress++,SCY+LY,xPixel).GetEnumerator();
            //bool fetcherFinished = false;

            while (xPixel < 160) {
                if (!Enabled) {
                    if (xPixel > 0)
                        SetPixel(xPixel, LY, clearColor);
                    ++xPixel;
                    ++delayDots;
                    yield return stat;
                    continue;
                }
                xWindow = xWindow || ((xPixel + 7 == WX) && (WX > 0)) || (WX == 0 && xPixel <= 0);
                bool doWindow = WindowEnable && yWindow && xWindow;

                fetcher.MoveNext();
                bool fetcherFinished = fetcher.Current.HasValue;

                if (doWindow && (!usingWindow)) {
                    usingWindow = true;
                    FIFO.ClearLastBits();
                    fetcher = FIFO.FetchTileData(windowAddress++,LY - WY, xPixel).GetEnumerator();
                    fetcherFinished = false;
                }
                if ((!WindowEnable) || WY > LY || WX > xPixel) { 
                    usingWindow = false;
                    xWindow = false;
                }

                if (FIFO.BackgroundPixels <= 8 && fetcherFinished) {
#pragma warning disable CS8629 
                    var pushPixel = fetcher.Current.Value;//fetcherFinished will only be true when this value is not null, but VS complains for some reason, so I have to disable the nullable warning here. This will never get reached if the value is null.
#pragma warning restore CS8629 
                    FIFO.EnqueuePixels(pushPixel);
                    fetcher = FIFO.FetchTileData(usingWindow ? windowAddress++ : backgroundAddress,usingWindow?WY+LY:SCX+LY, xPixel).GetEnumerator();
                    ++backgroundAddress;
                }
                if (FIFO.BackgroundPixels > 8) {
                    var pixel = FIFO.ShiftOut();
                    if (xPixel >= 0 && BGWindowPriority) {
                        SetPixel(xPixel, LY, pixel);
                    }
                    ++xPixel;
                }
                ++delayDots;
                yield return stat;
            }
            stat = PPUStatus.HBlank;
            if (Mode0InterruptEnable) GB.CPU.SetInterrupt(InterruptType.Stat);
            for (int i = 0; i < 456 - delayDots; i++) {
                yield return stat;
            }
        }
        protected ushort calculateBackgroundTileMapAddress()
        {
            return (ushort)(0x9800 | ((BackgroundTileMap ? 1 : 0) << 10) | (((SCY + LY) & 0xF8) << 2) | ((SCX & 0xF8) >> 3));
        }
        protected ushort calculateWindowTileMapAddress()
        {
            return (ushort)(0x9800 | ((WindowTileMap?1:0) << 10) | (((LY - WY) & 0xf8) << 2));

        }

        #endregion
        #region BlockRendering
        public void GetTileMapBlock(int x,int y, Span2D<byte> tiles,ushort tileMapAddr) {
            int address = tileMapAddr;
            var vram = GB.VRam;
            y = y * 32;
            for (int v = 0; v < tiles.Height; v++) {
                for (int u = 0; u < tiles.Width; u++) {
                    ushort addr = (ushort)(address + ((u + x)&31) + ((y + v)&31) * 32);
                    var data = vram.DirectRead((ushort)(addr-0x8000));
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
            return GB.VRam.DirectRead((ushort)(address+tile*16-0x8000),16);
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
            if (bitmap.Width < image.Width + x || bitmap.Height < image.Height + y) throw new ArgumentOutOfRangeException($"Bitmap is too small. It has to be at least {image.Width + x}x{image.Width + y}");
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
