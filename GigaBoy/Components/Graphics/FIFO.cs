using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//
//  Possible improvement: This is not certain, but its possible that .NET doesn't like using byte as the primary type, so the conversions might cause tiny slowdowns.
//  This would mean that all the uses of the byte type and the conversions to it are slowing down the code.
//  TODO: Research this more.
//
namespace GigaBoy.Components.Graphics
{
    public class FIFO
    {
        public PPU PPU { get; init; }
        public GBInstance GB { get; init; }
        public int BackgroundPixels { get; protected set; } = 0;
        public uint backgroundQueue = 0;
        public (byte, PaletteType)[] spriteQueue = new (byte, PaletteType)[8];
        public FIFO(PPU ppu) {
            GB = ppu.GB;
            PPU = ppu;
        }
        public void Reset() {
            backgroundQueue = 0;
            BackgroundPixels = 0;
            var cv = ((byte)0, PaletteType.Background);
            Array.Fill(spriteQueue,cv);
        }
        protected byte DequeuePixel() {
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
        public ColorContainer ShiftOut() {
            byte color = DequeuePixel();
            //System.Diagnostics.Debug.WriteLine($"queue: {Convert.ToString(backgroundQueue,2)}, pxl: {Convert.ToString(color,2)} ({color}) ");
            var palette = PaletteType.Background;
            if (spriteQueue[0].Item1 != 0) {
                color = spriteQueue[0].Item1;
                palette = spriteQueue[0].Item2;
            }
            for (int i = 1; i < 8; i++) spriteQueue[i - 1] = spriteQueue[i];
            spriteQueue[7] = (0, PaletteType.Background);
            return PPU.Palette.GetTrueColor(color,palette);
        }
        /*public IEnumerable<(byte, byte)?> FetchTileData(bool window)//Apparently the window only pauses the PPU for 8 dots as its pixels have to be loaded into the FIFO.
            //Disgarding each pixel from the FIFO does also take 1 dot, so SCX=5 would add 5 dots to mode 3
        {//Run this every time the pixelQueue.Length==8, so the data for the next pixel can be obtained while the FIFO is still processing the previous 8 pixels.
            yield return null;          //Older method for fetching tile data. This method has incorrect timings, and doesnt support fetching sprites. Don't use it.

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
        }//*/
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


        public IEnumerable<(byte, byte)?> FetchTileData(ushort tileMap,int scrollY,int px=0,bool fetchSprite=false) {
            yield return null;
            byte tileId = 0;
            CRAM cram;
            if (!fetchSprite) {
                //tileId = VRAM.DirectRead((ushort)(tileMap-0x8000));
                
                var tmram = GB.TMRAMBanks[(tileMap - 0x9800) / 0x400];
                tileId = tmram.DirectRead((ushort)((tileMap - 0x9800) % 0x400));
                
                //Test if the correct tile was fetched.
                Span2D<byte> til = new Span2D<byte>(stackalloc byte[1],1,1);
                PPU.GetTileMapBlock(px / 8, PPU.LY / 8, til, (ushort)(PPU.BackgroundTileMap ? 0x9c00 : 0x9800));
                //if (til[0, 0] != tileId) GB.Log($"Incorrect tile fetched ({tileId:X} was fetched, should be {til[0,0]:X})");
            }
            if (fetchSprite) {
                throw new NotImplementedException("Sprites have not been implemented yet.");
            }
            yield return null;
            ushort tileAddr;
            if (!PPU.TileData)
            {
                tileAddr = (ushort)((tileId << 4) | ((scrollY & 0x7) << 1));
            }
            else
            {
                tileAddr = (ushort)((0x1000 - (tileId << 4)) | ((scrollY & 0x7) << 1));
            }
            yield return null;
            byte tileDataLow=255;
            
            cram = GB.CRAMBanks[tileAddr / 0x800];
            tileDataLow = cram.DirectRead((ushort)(tileAddr % 0x800));
            
            yield return null;
            if (!PPU.TileData)
            {
                tileAddr = (ushort)((tileId << 4) | ((scrollY & 0x7) << 1));
            }
            else
            {
                tileAddr = (ushort)((0x1000 - (tileId << 4)) | ((scrollY & 0x7) << 1));
            }
            tileAddr = (ushort)(tileAddr | 1);
            yield return null;

            cram = GB.CRAMBanks[tileAddr / 0x800];
            byte tileDataHigh = cram.DirectRead((ushort)(tileAddr % 0x800));
            yield return (tileDataLow,tileDataHigh);
        }

        public void EnqueuePixels((byte,byte) data)
        {
            uint data1 = data.Item1;
            uint data2 = data.Item2;
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
