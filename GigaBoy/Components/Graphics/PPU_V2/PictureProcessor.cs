using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics.PPU_V2
{
    public class PictureProcessor
    {
        public GBInstance GB { get; init; }
        public PPU PPU { get; init; }

        public enum FifoState { Stopped, DummyT, Dummy0, Dummy1, Character, Plane0, Plane1, Sprite }
        public enum WindowMode { Off, Rendering, Glitch_A6, Glitch_00 }

        public bool Debug { get => PPU.Debug; set => PPU.Debug = value; }
        public byte LY { get => PPU.LY; }
        
        public ushort fetcher_address;
        public ushort character_address;
        public byte character_plane1;
        public byte character_plane2;
        public int xPixel = 0;
        private bool timerToggle = true;
        public FifoState State = FifoState.Stopped;
        public WindowMode LastWindowState = WindowMode.Off;

        public int BackgroundPixels { get; protected set; } = 0;
        public uint backgroundQueue = 0;
        public (byte, PaletteType)[] spriteQueue = new (byte, PaletteType)[8];

        public PictureProcessor(PPU ppu) {
            PPU = ppu;
            GB = PPU.GB;
            Reset();
        }

        public void Tick()
        {
            timerToggle = !timerToggle;
            if (BackgroundPixels > 8) ShiftOut();

            if (timerToggle) {
                switch (LastWindowState)
                {
                    case WindowMode.Off:
                        switch (State)
                        {
                            case FifoState.DummyT:
                                FIFO_Latch();
                                FIFO_B();
                                State = FifoState.Dummy0;
                                break;
                            case FifoState.Character:
                                FIFO_B();                           //Add window handling code here.
                                IncrementFetcherAddress();
                                State = FifoState.Plane0;
                                break;

                            case FifoState.Dummy0:
                                FIFO_0();
                                State = FifoState.Dummy1;
                                break;
                            case FifoState.Plane0:
                                FIFO_0();
                                State = FifoState.Plane1;
                                break;

                            case FifoState.Dummy1:
                                FIFO_1();
                                ClearPixelQueue();
                                State = FifoState.Character;
                                break;
                            case FifoState.Plane1:
                                FIFO_1();
                                State = FifoState.Sprite;
                                break;

                            case FifoState.Sprite:
                                FIFO_S();
                                State = FifoState.Character;
                                break;

                            case FifoState.Stopped:
                                break;
                        }
                        break;
                    case WindowMode.Rendering:
                        throw new NotImplementedException();
                        break;
                    case WindowMode.Glitch_00:
                        throw new NotImplementedException();
                        break;
                    case WindowMode.Glitch_A6:
                        throw new NotImplementedException();
                        break;
                }
                
            }
        }


        public void FIFO_B() {
            byte ntbyte = PPU.Fetch(fetcher_address);
            byte ysub = (byte)(PPU.SCY + LY);
            if (PPU.TileData)   //One of these tiling modes is bugged.
            {
                character_address = (ushort)((ntbyte << 4) | ((ysub & 0x7) << 1));
            }
            else
            {
                character_address = (ushort)((0x1000 - (ntbyte << 4)) | ((ysub & 0x7) << 1));
            }
            character_address |= 0x8000;
        }
        public void FIFO_W() {
            
        }
        public void FIFO_0() {
            character_plane1 = PPU.Fetch(character_address);
        }
        public void FIFO_1() {
            character_plane2 = PPU.Fetch((ushort)(character_address+1));
            EnqueuePixels(character_plane1,character_plane2);
        }
        public void FIFO_S() {

        }

        public void FIFO_Latch()
        {
            byte ybase = (byte)((PPU.SCY + LY) & 0xFF);   // calculates the effective vis. scanline
            fetcher_address = (ushort)((0x9800 | ((PPU.BackgroundTileMap ? 1 : 0) << 10) | ((ybase & 0xf8) << 2) | (PPU.SCX & 0xf8) >> 3));
            xPixel = -(PPU.SCX % 8);
        }
        public void IncrementFetcherAddress()
        {
            fetcher_address = (ushort)((fetcher_address & 0b1111111111100000) | ((fetcher_address + 1) & 0b0000000000011111));
        }


        public void EnqueuePixels(byte plane1, byte plane2)
        {
            uint data1 = plane1;
            uint data2 = plane2;
            ClearLastBits();
            BackgroundPixels += 8;
            for (int i = 0; i < 8; i++)
            {
                uint color = data1 & 1;
                color = color | ((data2 & 1) << 1);
                color = color << (i * 2);
                backgroundQueue = backgroundQueue | color;
                data1 = data1 >> 1;
                data2 = data2 >> 1;
            }
        }
        protected byte DequeuePixel()
        {
            if (BackgroundPixels <= 8) throw new DataMisalignedException("Cannot shift out pixels if the pixel FIFO contains 8 pixels or less.");
            const uint MASK = 0xC0000000;
            uint color = backgroundQueue & MASK;
            backgroundQueue = backgroundQueue << 2;
            --BackgroundPixels;
            return (byte)(color >> 30);
        }
        public void ClearLastBits()
        {
            backgroundQueue = backgroundQueue & 0xFFFF0000;
        }
        public void ShiftOut()
        {
            byte color = DequeuePixel();
            //System.Diagnostics.Debug.WriteLine($"queue: {Convert.ToString(backgroundQueue,2)}, pxl: {Convert.ToString(color,2)} ({color}) ");
            var palette = PaletteType.Background;
            /*      //Legacy Sprite Mixing Code
            if (spriteQueue[0].Item1 != 0)
            {
                color = spriteQueue[0].Item1;
                palette = spriteQueue[0].Item2;
            }
            for (int i = 1; i < 8; i++) spriteQueue[i - 1] = spriteQueue[i];
            spriteQueue[7] = (0, PaletteType.Background);
            */
            SetPixel(PPU.Palette.GetTrueColor(color, palette));
        }
        public void SetPixel(ColorContainer pixel) {
            if (xPixel < 0) {
                ++xPixel;
                return;
            }
            PPU.SetPixel(xPixel++,LY,pixel);
            if (xPixel == 160)
            {
                Reset();
                PPU.State = PPUStatus.HBlank;
                if (PPU.Mode0InterruptEnable) GB.CPU.SetInterrupt(InterruptType.Stat);
            }
        }

        public void ClearPixelQueue() {
            backgroundQueue = 0;
            BackgroundPixels = 0;
        }

        public void Reset()
        {
            timerToggle = true;
            xPixel = 0;
            ClearPixelQueue();
            State = FifoState.Stopped; 
            LastWindowState = WindowMode.Off;

            var cv = ((byte)0, PaletteType.Background);
            Array.Fill(spriteQueue, cv);
        }
        public void Start() {
            State = FifoState.DummyT;
        }
    }
}
