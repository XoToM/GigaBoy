using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics.PPU_V2
{
    public struct SpritePixelData
    {
        public byte Color { get; init; }
        public PaletteType Palette { get; init; }
        public bool Priority { get; init; }

        public ColorContainer GetColor(PPU ppu) {
            return ppu.Palette.GetTrueColor(Color,Palette);
        }
    }
}
