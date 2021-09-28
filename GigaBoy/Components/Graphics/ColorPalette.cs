using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public enum PaletteType : sbyte { Background,Sprite1,Sprite2 }
    public class ColorPalette
    {
        public readonly byte[,] Palettes = new byte[3, 4] { { 0, 1, 2, 3 }, { 0, 1, 2, 3 }, { 0, 1, 2, 3 } };
        public readonly Color[] TruePalette = new Color[4] {Color.Black,Color.DarkGray,Color.LightGray,Color.White };
        public Color GetTrueColor(byte colorIndex,PaletteType type) {
            unchecked
            {
                if (type != PaletteType.Background && colorIndex == 0) return Color.Transparent;
                colorIndex = (byte)(colorIndex & 0b11);
                sbyte btype = (sbyte)(((sbyte)type)&0b11);
                if (btype > 2) return Color.Blue;
                return TruePalette[Palettes[btype,colorIndex]];
            }
        }
        public void SetColor(byte colorIndex, PaletteType type,byte color) {
            unchecked
            {
                sbyte btype = (sbyte)type;
                if (btype > 2) return;
                colorIndex = (byte)(colorIndex & 0b11);
                if (colorIndex == 0 && btype != 0) return;
                color = (byte)(color & 0b11);
                Palettes[btype, colorIndex] = color;
            }
        }
    }
}
