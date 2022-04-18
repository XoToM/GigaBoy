using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
	/// <summary>
	/// Character RAM Component
	/// </summary>
	public class CRAM : RAM
	{
		public readonly bool[] ModifiedCharacters = new bool[128];
		public bool Modified { get; set; } = false;
		public CRAM(GBInstance gb) : base(gb, 128*16)
		{

		}
		public override bool Available() {
			return true;
			//Currently always true
			if (!((GB.PPU.State != PPUStatus.GenerateFrame) || !GB.PPU.Enabled)) {
				GB.Log($"!w ppu = {GB.PPU.State},  enabled={GB.PPU.Enabled}, LY={GB.PPU.LY:X}");
				return false;
			}
			return (GB.PPU.State != PPUStatus.GenerateFrame) || !GB.PPU.Enabled;
		}
		public override void DirectWrite(ushort address, byte value)
		{
			base.DirectWrite(address, value);
			ModifiedCharacters[address / 16] = true;
			Modified = true;
		}
		public void GetCharacter(ref Span2D<ColorContainer> span2D, int characterID, ColorPalette palette, PaletteType paletteType)
		{
			Span<ColorContainer> charData = span2D.Buffer;
			if (charData.Length < 8 * 8) throw new InsufficientMemoryException();
			palette.ToColors(Memory.AsSpan(characterID * 16, 16), charData, paletteType);
			for (int i = 0; i < charData.Length; i += 8) {
				charData.Slice(i, 8).Reverse();
			}
		}
		public void GetCharacter(ref Span<ColorContainer> charData, int characterID, ColorPalette palette, PaletteType paletteType)
		{
			if (charData.Length < 8 * 8) throw new InsufficientMemoryException();
			palette.ToColors(Memory.AsSpan(characterID * 16, 16), charData, paletteType);
		}
	}
}
