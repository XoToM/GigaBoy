using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    /// <summary>
    /// OAM Ram
    /// </summary>
    public class OamRam : RAM, IEnumerable<OamSprite>
    {
        public OamSprite[] SpriteData = new OamSprite[40];
        public bool Modified { get; set; } = false;
        public OamRam(GBInstance gb) : base(gb,4*40){
            
        }
        public override bool Available()
        {
            return (GB.PPU.State != PPUStatus.GenerateFrame) || (GB.PPU.State != PPUStatus.OAMSearch) || !GB.PPU.Enabled;
        }
        public override void DirectWrite(ushort address, byte value)
        {
            //base.DirectWrite(address, value);
            var prop = address % 4;
            var entry = GetOamEntry(address / 4);
            switch (prop)
            {
                case 0:
                    entry = entry with { PosY = value };
                    break;
                case 1:
                    entry = entry with { PosX = value };
                    break;
                case 2:
                    entry = entry with { TileID = value };
                    break;
                case 3:
                    entry = entry with { Attributes = value };
                    break;
            }
            SpriteData[address / 4] = entry;
            Modified = true;
        }
        public void GetTileMap(ref Span2D<byte> tilemap,int x,int y) {
            if ((y + tilemap.Height) > 32 || tilemap.Width + x > 32) throw new InsufficientMemoryException();
            var tm = new Span2D<byte>(Memory.AsSpan(),32,32);
            for (int i = 0; i < tilemap.Height; i++) {
                tm.GetBlockHorizontal(x,y+i,tilemap.Buffer.Slice(y*tilemap.Width+x,tilemap.Width));
            }
        }
        public OamSprite GetOamEntry(int index) {
            //int baseAddress = base.DirectRead(index * 4);
            //return new OamSprite() { PosY = DirectRead(baseAddress), PosX = DirectRead(baseAddress + 1), TileID = DirectRead(baseAddress + 2), Attributes = DirectRead(baseAddress + 3) };
            return SpriteData[index];
        }
        public override byte DirectRead(ushort address)
        {
            if (address >= 0xA0) return 0;
            //return base.DirectRead(address);
            var entry = GetOamEntry(address / 4);
            var prop = address % 4;
            switch (prop) {
                case 0:
                    return entry.PosY;
                case 1:
                    return entry.PosX;
                case 2:
                    return entry.TileID;
                case 3:
                    return entry.Attributes;
            }
            //  This error literally cannot happen unless the computer this program is on is unstable and/or skips instructions. address cannot be negative, and the mod operation forces the values to be in range of 0-3.
            throw new InvalidOperationException("Memory memory corruption or system instability detected. This error cannot appear on a properly functioning machine.");
        }

        public IEnumerator<OamSprite> GetEnumerator()
        {
            for (int i = 0; i < 40; i++) {
                yield return GetOamEntry(i); 
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    public readonly struct OamSprite {
        public byte PosX { get; init; }
        public byte PosY { get; init; }
        public byte Attributes { get; init; }
        public byte TileID { get; init; }

        public bool BGPriority { get => (Attributes & 128) != 0; }
        public bool YFlip { get => (Attributes & 64) != 0; }
        public bool XFlip { get => (Attributes & 32) != 0; }
        public PaletteType Palette { get => ((Attributes & 16) != 0) ? PaletteType.Sprite2 : PaletteType.Sprite1; }
    }
}
