using GigaBoy;
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
    public partial class Form1 : Form
    {
        public const int BORDER_WIDTH = 10;
        public const int BORDER_HEIGHT = 10;
        public GBInstance GB = new();
        private int _gameImageScale;

        public int GameWindowScale
        {
            get { return _gameImageScale; }
            set { 
                _gameImageScale = value; 
                Width = 160 * _gameImageScale + BORDER_WIDTH * 4;
                Height = 144 * _gameImageScale + BORDER_HEIGHT * 3 + TitleBarHeight;
                Invalidate(); }
        }
        public int TitleBarHeight { get {
                Rectangle screenRectangle = RectangleToScreen(ClientRectangle);
                return screenRectangle.Top - Top;
            } }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Random rng = new();
            for (ushort a = 0x8000; a < 0xA000; a++) {
                GB.VRam.DirectWrite(a,(byte)(rng.Next()&0xFF));
            }
            GB.PPU.LCDC = GB.PPU.LCDC | GigaBoy.Components.Graphics.LCDCFlags.PPUEnable| GigaBoy.Components.Graphics.LCDCFlags.WindowEnable;
            GB.PPU.WY = 64;
            GameWindowScale = 3;//Draws the image
            MLoop();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(GB.PPU.GetInstantImage(),BORDER_WIDTH,BORDER_HEIGHT,160 * GameWindowScale, 144 * GameWindowScale);
        }
        public async void MLoop()
        {
            while (true) {
                await Task.Delay(16);
                GB.PPU.WX++;
                GB.PPU.SCX++;
                Invalidate();
            }
        }
    }
}
