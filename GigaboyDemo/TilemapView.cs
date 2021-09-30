using GigaBoy;
using GigaBoy.Components;
using GigaBoy.Components.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GigaboyDemo
{
    public partial class TilemapView : UserControl
    {
        public Bitmap Frame { get; protected set;}
        public bool DrawScreen { get; set; }
        private ushort _address;
        private ushort _tilemap;
        private GBInstance? gb;
        private int scaling;

        public ushort TileMap
        {
            get { return _address; }
            set { _address = value; Redraw(); }
        }
        public ushort TileData
        {
            get { return _tilemap; }
            set { _tilemap = value; Redraw(); }
        }
        public GBInstance? GB
        {
            get { return gb; }
            set { gb = value; Redraw(); }
        }

        public int Scaling
        {
            get { return scaling; }
            set { 
                scaling = value;
                Width = 256 * scaling;
                Height = 256 * scaling;
                Redraw();
            }
        }


        public TilemapView()
        {
            Frame = new(32 * 8, 32 * 8);
            InitializeComponent();
        }

        private void TilemapViewer_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            if (gb == null) {
                return; 
            }
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.DrawImage(Frame,0,0,Width,Height);
            if (DrawScreen) {
                var x = gb.PPU.SCX;
                var y = gb.PPU.SCY;
                g.DrawRectangle(Pens.Blue,(x)*scaling,(y) * scaling, 160 * scaling, 144 * scaling);
                g.DrawRectangle(Pens.Blue,(x-256)*scaling,(y) * scaling, 160 * scaling, 144 * scaling);
                g.DrawRectangle(Pens.Blue,(x)*scaling,(y-256) * scaling, 160 * scaling, 144 * scaling);
                g.DrawRectangle(Pens.Blue,(x-256)*scaling,(y-256) * scaling, 160 * scaling, 144 * scaling);
            }
        }

        private void TilemapViewer_Load(object sender, EventArgs e)
        {
            if (GBInstance.LastInstance != null)
            {
                gb = GBInstance.LastInstance;
                Redraw();
            }
        }
        public void Redraw() {
            if (gb == null) return;
            var ppu = gb.PPU;
            /*var address = TileMap;
            for (int y = 0; y < 32; y++) {
                for (int x = 0; x < 32; x++)
                {
                    byte data = gb.VRam.DirectRead(address++);
                    ushort a = (ushort)((data>127)?0x8800:TileData+data);
                    //This would draw a character map, not a tilemap. 
                    //ppu.GetTileAt
                    ppu.ForceDrawTile(a,Frame,x*8,y*8,PaletteType.Background);
                }
            }//*/
            var imgData = new Span2D<ColorContainer>(stackalloc ColorContainer[256*256], 256,256);
            ppu.DrawTileMap(imgData,0,0,32,32,TileMap,TileData,PaletteType.Background);
            ppu.DrawBitmap(imgData,Frame,0,0);
            Invalidate();
        }
    }
}
