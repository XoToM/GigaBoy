using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    /// <summary>
    /// Tile Map Ram
    /// </summary>
    public class TMRAM : RAM
    {
        public bool Modified { get; set; } = false;
        public TMRAM(GBInstance gb) : base(gb,32*32){
            
        }
        public override bool Available()
        {
            return GB.PPU.State != PPUStatus.GenerateFrame || !GB.PPU.Enabled;
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
        public override byte DirectRead(ushort address)
        {
            //System.Diagnostics.Debug.WriteLine($"Vram Read from {address:X}");
            return base.DirectRead(address);
        }
    }
}
