using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public class FIFO
    {
        public GBInstance GB { get; init; }
        public PPU PPU { get; init; }
        public Queue<Color> PixelQueue = new Queue<Color>(16);

        public FIFO(GBInstance gb) { 
            GB = gb;
            PPU = gb.PPU;
        }


        
    }
}
