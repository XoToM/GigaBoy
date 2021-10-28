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
        public TilemapViewer TilemapViewer;
        public RegisterViewer RegisterViewer;
        public TileDataViewer TileDataViewer;
        public Debugger Debugger;
        public string? setRomFile = null;
        public GBInstance GB { get; protected set; }
        private bool _gbPaused = false;

        public bool GBPaused
        {
            get { return _gbPaused; }
            set { 
                _gbPaused = value;
                if (value) GB.Stop();
            }
        }

        private int _gameImageScale;
        private Task? gbProcessor = null;

        public int GameWindowScale
        {
            get { return _gameImageScale; }
            set { 
                _gameImageScale = value;
                var w = 160 * _gameImageScale + BORDER_WIDTH * 4;
                var h = 144 * _gameImageScale + BORDER_HEIGHT * 3 + TitleBarHeight;
                MinimumSize = new Size(w, h);
                MaximumSize = new Size(w, h);
                Width = w;
                Height = h;
                Invalidate(); }
        }
        public int TitleBarHeight { get {
                Rectangle screenRectangle = RectangleToScreen(ClientRectangle);
                return screenRectangle.Top - Top;
            } }

        public Form1()
        {
            GB = new();
            InitializeComponent();
            TilemapViewer = new();
            TilemapViewer.Show();
            TileDataViewer = new();
            TileDataViewer.Show();
            RegisterViewer = new(GB, this);
            RegisterViewer.Show();
            Debugger = new(GB);
            Debugger.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /*Random rng = new();
            //for (ushort a = 0x9800; a < 0xA000; a++){
                for (ushort a = 0x8000; a < 0xA000; a++) {
                //GB.VRam.DirectWrite(a,(byte)(rng.Next()&0xFF&1));
                GB.VRam.DirectWrite(a,(byte)(rng.Next()&0xFF));
            }
            for (ushort a = 0x8000; a < 0x8010; a++)
            {
                GB.VRam.DirectWrite(a, 0);
            }
            for (ushort a = 0x8010; a < 0x8020; a++)
            {
                GB.VRam.DirectWrite(a, 0xFF);
            }//*/
            GB.PPU.Enabled = true;
            //GB.PPU.WindowEnable = true;
            //GB.PPU.WY = 64;
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
                
                
                await Task.Delay(15);

                if (setRomFile is not null)
                {
                    TilemapViewer.Close();
                    TileDataViewer.Close();
                    RegisterViewer.Close();
                    GB.Stop();
                    if(gbProcessor is not null) await gbProcessor;
                    
                    GB = new GBInstance(setRomFile);
                    GB.CPU.Debug = true;
                    //GB.CPU.PrintOperation = true;
                    GBPaused = true;

                    TilemapViewer = new();
                    TilemapViewer.Show();
                    TileDataViewer = new();
                    TileDataViewer.Show();
                    RegisterViewer = new(GB, this);
                    RegisterViewer.Show();
                    Debugger = new(GB);
                    Debugger.Show();
                    setRomFile = null;
                    gbProcessor = Task.Run(runGB);
                }

                if (gbProcessor is null) continue;

                Invalidate();
                TilemapViewer.tm.Redraw();
                TileDataViewer.tv.Redraw();
                RegisterViewer.RefreshRegisters();
            }
        }
        private async Task runGB()
        {
            try
            {
                while (true)
                {
                    if(!GBPaused)GB.MainLoop();
                    await Task.Delay(10);
                }
            }
            catch (Exception e)
            {
                GB.Log($"Error in opcode {GB.CPU.LastOpcode:X}        PC = {GB.CPU.PC:X}");
                GB.Log(e.ToString());
                MessageBox.Show(e.ToString(), e.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        public void StepGB()
        {
            if (!GBPaused) return;
            try
            {
                GB.Step();
            }
            catch (Exception e)
            {
                GB.Log($"Error in opcode {GB.CPU.LastOpcode:X}        PC = {GB.CPU.PC:X}");
                GB.Log(e.ToString());
                MessageBox.Show(e.ToString(), e.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Any()) {
                setRomFile = files.First();
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
    }
}
