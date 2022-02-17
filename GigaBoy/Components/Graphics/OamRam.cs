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
        public bool Modified { get; set; } = false;
        public OamRam(GBInstance gb) : base(gb,4*40){
            
        }
        public override bool Available()
        {
            return (GB.PPU.State != PPUStatus.GenerateFrame) || (GB.PPU.State != PPUStatus.OAMSearch) || !GB.PPU.Enabled;
        }
        public override void DirectWrite(ushort address, byte value)
        {
            base.DirectWrite(address, value);
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
            int baseAddress = base.DirectRead(index * 4);
            return new OamSprite() { PosY = DirectRead(baseAddress), PosX = DirectRead(baseAddress + 1), TileID = DirectRead(baseAddress + 2), Attributes = DirectRead(baseAddress + 3) };
        }
        public override byte DirectRead(ushort address)
        {
            if (address >= 0xA0) return 0;
            return base.DirectRead(address);
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
    public struct OamSprite {
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
