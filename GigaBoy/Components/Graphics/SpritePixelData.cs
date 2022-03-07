using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public struct SpritePixelData
    {
        public GBInstance GB { get; init; }
        public PPU PPU { get => GB.PPU; }
        public byte Color { get; init; }
        public PaletteType Palette { get; init; }
        public bool BGPriority { get; init; }
        public static void MixSprite(PPU ppu, byte plane1, byte plane2, OamSprite sprite) {
            if (!ppu.ObjectEnable) return;
            var pixelQueue = ppu.PictureProcessor.spritePixelQueue;
            for (int i = 0; i < 8; i++) {
                var j = sprite.XFlip ? i : (7 - i);

                byte color = (byte)(((plane1 >> j) & 1) | (((plane2 >> (j)) << 1) & 2));
                var palette = sprite.Palette;
                var priority = sprite.BGPriority;
                
                var pixelData = new SpritePixelData() { Color = color, GB = ppu.GB, Palette = palette, BGPriority = priority };

                if (pixelQueue.Count <= i)
                {
                    pixelQueue.Enqueue(pixelData);
                }
                else 
                {
                    if (color != 0 && pixelQueue[i].Color == 0) pixelQueue[i] = pixelData;
                }
            }
        }
    }
}
