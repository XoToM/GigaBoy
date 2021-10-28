using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using GigaBoy;
using GigaBoy.Components;
using GigaBoy.Components.Graphics;

namespace GigaBoy_WPF
{
    public static class Emulation       //This class connects the UI and the emulator together, and provides helper methods for things such as rendering.
    {
        private static GBInstance? _gBInstance;
        private static Task? _gbRunner;

        public static GBInstance? GB
        {
            get { return _gBInstance; }
            set { _gBInstance = value; GigaboyRefresh?.Invoke(null, EventArgs.Empty); }
        }
        public static string? RomFilePath { get; private set; }
        public static WriteableBitmap VisibleImage { get; private set; } = new(160,144,96,96,System.Windows.Media.PixelFormats.Bgra32,null);


        public static event EventHandler? GigaboyRefresh;
        public static event EventHandler? GBFrameReady;

        public static void Reset(string? romPath) {
            Stop();
            RomFilePath = romPath;
            if (romPath is not null)
            {
                GB = new GBInstance(romPath);
            }
            else {
                GB = new GBInstance();
            }
            GB.CPU.Debug = true;
            Start();
        }
        public static void Start() {
            //Prepare and start the thread which will run the mainLoop() method.
            if (GB is null ||_gbRunner is null|| GB.Running) return;
            _gbRunner = Task.Run(runMainLoop);
        }
        private static void runMainLoop()
        {//This should be ran on a separate thread
            //Call GB.MainLoop() here once. If it returns we should notify the UI thread, and return.
            GigaboyRefresh?.Invoke(null, EventArgs.Empty);
            if (GB is not null) GB.MainLoop();
            GigaboyRefresh?.Invoke(null, EventArgs.Empty);
        }
        public static void Stop() {
            if (GB is not null && _gbRunner is not null && GB.Running)
            {
                GB.Stop();
                _gbRunner.Wait();
            }
        }

        public static void Render() {
            if (GB is null) return;
            lock (GB.PPU)
            {
                DrawGB(VisibleImage, GB.PPU.GetFrame(), 0, 0);
            }
            GBFrameReady?.Invoke(null,EventArgs.Empty);
        }

        public static void DrawGB(WriteableBitmap bitmap, Span2D<ColorContainer> image, int x, int y) {
            if (bitmap.BackBufferStride < image.Width + x || bitmap.Height < image.Height + y) throw new ArgumentOutOfRangeException($"Bitmap is too small. It has to be at least {image.Width + x}x{image.Height + y}");
            bitmap.Lock();
            try
            {
                unsafe
                {
                    
                    Span2D<int> data = new Span2D<int>(new Span<int>((void*)bitmap.BackBuffer, bitmap.BackBufferStride * bitmap.PixelHeight), bitmap.BackBufferStride, bitmap.PixelHeight);
                    for (int v = 0; v < image.Height; v++)
                    {
                        for (int u = 0; u < image.Width; u++)
                        {
                            data[v + y, u + x] = image[v, u].ARGB;
                        }
                    }
                }
                bitmap.AddDirtyRect(new System.Windows.Int32Rect(x,y,image.Width,image.Height));
            }
            finally {
                bitmap.Unlock();
            }
        }

    }
}
