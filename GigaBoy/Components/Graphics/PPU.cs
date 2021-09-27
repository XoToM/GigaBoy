using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
    public enum PPUStatus : sbyte {Off=-1,HBlank,VBlank,OAMSearch,GeneratePict }
    public class PPU
    {
        public GBInstance GB { get; init; }
        public int Dot { get; protected set; }
        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public Bitmap? CurrentImage { get; protected set; }
        public PPUStatus State { get; protected set; } = PPUStatus.Off;

        public PPU(GBInstance gb,int width, int height) {
            Width = width;
            Height = height;
            GB = gb;
        }
        public 
    }
}
