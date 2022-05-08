using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public enum PaletteType : sbyte { Background, Sprite1, Sprite2, Debug }
    /// <summary>
    /// ColorContainer stores System.Drawing.Color as an int, so it can be easily used with spans, and stackalloc.
    /// </summary>
    public readonly struct ColorContainer
    {
        private readonly int color;
        public Color Color { get { return Color.FromArgb(color); } init { color = value.ToArgb(); } }
        public int ARGB { get { return color; } init { color = value; } }
        public ColorContainer(Color color) {
            this.color = color.ToArgb();
        }
        public ColorContainer(byte r, byte g, byte b)
        {
            this.color = b | (g << 8) | (r << 16) | (0xFF << 24);
        }
        public ColorContainer(byte r, byte g, byte b,byte a)
        {
            this.color = b | (g << 8) | (r << 16) | (a << 24);
        }

        public static implicit operator Color(ColorContainer container) { return container.Color; }
        public static implicit operator ColorContainer(Color color) { return new ColorContainer(color); }
    }
    public class ColorPalette
    {
        //The 4 colour palettes of 4 colours each. The 4th palette is the debug palette, which is read-only.
        //The palettes are defined in the following order:   Background,     Sprite 1         Sprite 2        Debug
        public readonly byte[,] Palettes = new byte[4, 4] { { 0, 1, 2, 3 }, { 0, 1, 2, 3 }, { 0, 1, 2, 3 }, { 4, 4, 5, 6 } };
        //The palette of predefined colours. It defines 7 unique colours used by the emulator, including colours used for debugging. Having the debug palette allowed me to easily add debug features directly in the rendering classes, instead of applying them as post-processing effects.
        public readonly Color[] TruePalette = new Color[7] { Color.FromArgb(0xFF,0xFF,0xFF), Color.FromArgb(0xAA,0xAA,0xAA), Color.FromArgb(0x55,0x55,0x55), Color.FromArgb(0, 0, 0), Color.FromArgb(0, 0, 255), Color.FromArgb(0, 255, 0), Color.DeepPink };
        public Color GetTrueColor(byte colorIndex, PaletteType type) {
            unchecked   //Remove overflow checks to increase speed
            {
                //if ((type != PaletteType.Background || type != PaletteType.Debug) && colorIndex == 0) return Color.Transparent;
                var cIndex = colorIndex & 0b11;     //Clamp the values to be between 0 and 3, which just so happens to be within the bounds of the Palette array
                var btype = ((int)type) & 0b11;//Clamp the values to be between 0 and 3, which also just so happens to be within the bounds of the Colour Palette
                return TruePalette[Palettes[btype,cIndex]];//Return the color at the clamped indexes
            }
        }
        public void SetColor(byte colorIndex, PaletteType type,byte color) {
            unchecked
            {
                sbyte btype = (sbyte)type;
                if (btype > 2) return;
                colorIndex = (byte)(colorIndex & 0b11);
                color = (byte)(color & 0b11);
                Palettes[btype, colorIndex] = color;
            }
        }
        public void ToColors(Span<byte> bytes, Span<ColorContainer> dest, PaletteType palette)
        {
            if (bytes.Length % 2 == 1) bytes.Slice(0, bytes.Length - 1);
            for (int i = 0; i < bytes.Length * 4; i++)
            {
                var shift = i % 8;
                var index = (i / 8) * 2;
                var mask = 1 << shift;
                var data1 = (bytes[index] & mask) >> shift;
                var data2 = ((bytes[index + 1] & mask) >> shift) << 1;
                var color = GetTrueColor((byte)(data1 | data2), palette);
                dest[i] = color;
            }
        }

        internal void SetPaletteByte(PaletteType paletteType, byte value)
        {
            if (paletteType == PaletteType.Debug) return;
            var palette = (sbyte)paletteType;
            Palettes[palette, 3] = (byte)((value & 0b11000000) >> 6);
            Palettes[palette, 2] = (byte)((value & 0b00110000) >> 4);
            Palettes[palette, 1] = (byte)((value & 0b00001100) >> 2);
            Palettes[palette, 0] = (byte)((value & 0b00000011));
        }

        internal byte GetPaletteByte(PaletteType paletteType)
        {
            if(paletteType == PaletteType.Debug) return 0;  //The debug colour palette is not meant to be accessed through registers

            var palette = (sbyte)paletteType; //Get the index into the table of colour palettes

            //Convert the colours in the colour palette to 4 2-bit values and return them as a single byte.
            var result = Palettes[palette, 3] << 6; 
            result |= Palettes[palette, 2] << 4;
            result |= Palettes[palette, 1] << 2;
            result |= Palettes[palette, 0];

            return (byte)result;
        }
    }
}
