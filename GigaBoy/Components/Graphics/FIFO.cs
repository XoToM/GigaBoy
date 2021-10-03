using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public class FIFO
    {
        public PPU PPU { get; init; }
        public GBInstance GB { get; init; }
        public RAM VRAM { get; init; }
        public Queue<byte> backgroundQueue = new(16);
        public (byte, PaletteType)[] spriteQueue = new (byte, PaletteType)[8];
        public int TileXOffset = 0;
        public FIFO(PPU ppu) {
            GB = ppu.GB;
            VRAM = GB.VRam;
            PPU = ppu;
        }
        public void Reset() {
            backgroundQueue.Clear();
            var cv = ((byte)0, PaletteType.Background);
            Array.Fill(spriteQueue,cv);
            TileXOffset = 0;
        }
        public ColorContainer ShiftOut() {
            byte color = backgroundQueue.Dequeue();
            var palette = PaletteType.Background;
            if (spriteQueue[0].Item1 != 0) {
                color = spriteQueue[0].Item1;
                palette = spriteQueue[0].Item2;
            }
            for (int i = 1; i < 8; i++) spriteQueue[i - 1] = spriteQueue[i];
            spriteQueue[7] = (0, PaletteType.Background);
            return PPU.Palette.GetTrueColor(color,palette);
        }
        public IEnumerable<(byte, byte)?> FetchTileData(bool window)//Apparently the window only pauses the PPU for 8 dots as its pixels have to be loaded into the FIFO.
            //Disgarding each pixel from the FIFO does also take 1 dot, so SCX=5 would add 5 dots to mode 3
        {//Run this every time the pixelQueue.Length==8, so the data for the next pixel can be obtained while the FIFO is still processing the previous 8 pixels.
            yield return null;

            int y = (PPU.SCY+PPU.LY)&0xFF;
            int oy = y % 8;
            y = y / 8;
            ushort address = 0x9800;
            if (!window&&PPU.BackgroundTileMap) address = 0x9C00;
            if (window&&PPU.WindowTileMap) address = 0x9C00;
            int x = ((PPU.SCX / 8) + TileXOffset++) & 0x1F;
            address += (ushort)(y * 32 + x);
            byte tileId = VRAM.DirectRead(address);

            yield return null;
            yield return null;

            address = 0x8000;
            if (PPU.TileData) address += 0x0800;
            if (tileId > 127) address = 0x8000;
            address += (ushort)(tileId * 16 + oy * 2);

            byte dataLow = VRAM.DirectRead(address);
            yield return null;
            yield return null;
            address = 0x8000;
            if (PPU.TileData) address += 0x0800;
            if (tileId > 127) address = 0x8000;
            address += (ushort)(tileId * 16 + oy * 2);

            byte dataHigh = VRAM.DirectRead(++address);
            yield return (dataLow, dataHigh);
        }
        /*public IEnumerable<bool> ProcessSprite(int spriteIndex)   //drawing sprites stops the FIFO for 6 dots so the sprite's data can be fetched
        {//Run this every time the pixelQueue.Length==8, so the data for the next pixel can be obtained while the FIFO is still processing the previous 8 pixels.
            yield return false;
            
            //Read sprite's tiledata and flags (unlike vram, OAM has a 16 bit bus to the ppu, so reading 2 bytes from it only takes 2 dots. Reading both tiledata and flags occurs at the same time)
            //There is apparently more delay (apparently 5 dots) that can occur per sprite which occurs because FIFO is stopped (todo: research this penalty more)

            int y = PPU.LY / 8;
            int oy = PPU.LY % 8;
            ushort address = 0xFE00;
            address += (ushort)(y * 32 + x);
            byte tileId = VRAM.DirectRead(address);//Fetch tileID of the sprite from OAM

            yield return false;
            yield return false;

            address = 0x8000;
            if (PPU.TileData) address += 0x0800;
            if (tileId > 127) address = 0x8000;
            address += (ushort)(tileId * 16 + oy * 2);

            byte dataLow = VRAM.DirectRead(address);
            yield return false;
            yield return false;
            byte dataHigh = VRAM.DirectRead(address);
            MixSprite((dataLow,dataHigh),palette);//Pass in the sprite's palette
            yield return true;
        }//*/
        public void EnqueuePixels((byte,byte) data)
        {
            const byte mask = 0b10000000;
            byte data1 = data.Item1;
            byte data2 = data.Item2;
            for (int i = 0; i < 8; i++)
            {
                byte color = (byte)((data1 & mask) >> 7);
                color = (byte)(color | ((data2 & mask) >> 6));
                backgroundQueue.Enqueue(color);
                data1 = (byte)(data1 << 1);
                data2 = (byte)(data2 << 1);
            }
        }
        public void MixSprite((byte, byte) data,PaletteType palette) {
            const byte mask = 0b10000000;
            var data1 = data.Item1;
            var data2 = data.Item2;
            for (int i = 0; i < 8; i++) {
                byte pixel = (byte)((data1&mask) >> 7);
                pixel = (byte)(pixel|((data2&mask) >> 6));
                if (spriteQueue[i].Item1 == 0) spriteQueue[i] = (pixel, palette);
                data1 = (byte)(data1 << 1);
                data2 = (byte)(data2 << 1);
            }
        }
    }
}
