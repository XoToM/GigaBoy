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
    public partial class TileDataView : UserControl
    {
        public GBInstance? gb;
        public Bitmap Frame { get; protected set; }
        private int _displaySections=3;
        private int _displaySectionsStart=0;
        private int _scaling=0;

        public int Scaling
        {
            get { return _scaling; }
            set { _scaling = value; Width = 128 * value;Height = 64 * DisplayTileDataSectionsCount * value; Redraw(); }
        }
        public int DisplayTileDataSectionsCount
        {
            get { return _displaySections; }
            set { _displaySections = value; Frame = new Bitmap(128, 64 * value); Redraw(); }
        }
        public int DisplayTileDataSections
        {
            get { return _displaySectionsStart%DisplayTileDataSectionsCount; }
            set { _displaySectionsStart = value; Redraw(); }
        }

        public TileDataView()
        {
            Frame = new Bitmap(128,64*DisplayTileDataSectionsCount);
            InitializeComponent();
        }

        private void TileDataViewer_Load(object sender, EventArgs e)
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
            /*ushort address = 0x8000;
            address += (ushort)((DisplayTileDataSections) * 16 * 128);
            for (int s = DisplayTileDataSections; s < DisplayTileDataSectionsCount; s++) {
                for (int y = 0; y < 8; y++) {
                    for (int x = 0; x < 16; x++) {
                        ppu.ForceDrawTile(address, Frame, x * 8, (y + (s * 8)) * 8, PaletteType.Background);
                        address += 16;
                    }
                }
            }//*/
            Span2D<byte> tilemap = new(stackalloc byte[128],16,8);
            for (int i = 0; i < 128; i++) tilemap.Buffer[i] = (byte)i;

            var imgData = new Span2D<ColorContainer>(stackalloc ColorContainer[128*8*8*DisplayTileDataSectionsCount], 128, 64*DisplayTileDataSectionsCount);

            int s = DisplayTileDataSections;
            for(int sc=0;sc<DisplayTileDataSectionsCount;sc++)
                ppu.DrawRegion(tilemap,new Span2D<ColorContainer>(imgData.Buffer.Slice(sc*128*64,128*64),128,64),(ushort)(0x8000+0x0800*s++),PaletteType.Background);
            ppu.DrawBitmap(imgData,Frame,0,0);
            Invalidate();
        }

        private void TileDataViewer_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            if (gb == null) {
                g.Clear(Color.White);
                return;
            }
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.DrawImage(Frame,0,0, 128 * _scaling, 64* DisplayTileDataSectionsCount * _scaling);
        }
    }
}
