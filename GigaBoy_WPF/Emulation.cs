using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using GigaBoy;
using GigaBoy.Components;
using GigaBoy.Components.Graphics;
using System.Windows.Input;
using GigaBoy_WPF.Components;

namespace GigaBoy_WPF
{
    public static class Emulation       //This class connects the UI and the emulator together, and provides helper methods for things such as rendering.
    {
        public static ICommand EmulatorControlCommand { get; private set; } = new EmulatorCommandRunner();
        private static GBInstance? _gBInstance;
        private static Task? _gbRunner;

        public static GBInstance? GB
        {
            get { return _gBInstance; }
            set { 
                _gBInstance = value;
                if (value is not null)
                {
                    if (GBArgs is null)
                    {
                        GBArgs = new(value);
                    }
                    else
                    {
                        GBArgs.GB = value;
                    }
                }
                else {
                    GBArgs = null;
                }
                GigaboyRefresh?.Invoke(null, EventArgs.Empty); 
            }
        }
        public static gbEventArgs? GBArgs { get; private set; } = null;
        public class gbEventArgs : EventArgs {
            public GBInstance GB { get; protected internal set; }
            public gbEventArgs(GBInstance gb) {
                GB = gb;
            }
        }

        public static string? RomFilePath { get; private set; }
        public static WriteableBitmap VisibleImage { get; private set; } = new(160,144,96,96,System.Windows.Media.PixelFormats.Bgra32,null);


        public static event EventHandler? GigaboyRefresh;
        /// <summary>
        /// This event is invoked when the emulator finishes drawing a frame. It is called from the UI thread, and it locks the GBInstance object, so all of its members can be safely accessed inside the event handler.
        /// </summary>
        public static event EventHandler<gbEventArgs>? GBFrameReady;

        public static void Restart(string? romPath) {
            Stop();
            Init(romPath);


            GB?.AddBreakpoint(0xFF4D, new() { BreakOnRead = true });

            Start();
        }
        public static void Init(string? romPath) {
            RomFilePath = romPath;
            if (romPath is not null)
            {
                GB = new GBInstance(romPath);
            }
            else
            {
                GB = new GBInstance();
            }
            GB.Breakpoint += GB_Breakpoint;
            GB.CPU.Debug = true;
            GB.CPU.PrintOperation = true;   //Warning: Setting this to true while BacklogOnlyLogging is set to false will defenestrate performance. Enable at your own risk!
            GB.DebugLogging = true;   //Warning: Setting this to true might defenestrate performance. Enable at your own risk!
            //GB.BacklogOnlyLogging = false;
            GB.PPU.FrameRendered += OnFrame;
            //GB.Clock.SpeedMultiplier = 100000;
        }

        private static void GB_Breakpoint(object? sender, EventArgs e)
        {
            MainWindow.Main?.Dispatcher.InvokeAsync(Stop);
            Debug.WriteLine("Breakpoint Hit");
        }

        public static void Start() {
            //Prepare and start the thread which will run the mainLoop() method.
            if (GB is null || _gbRunner is not null|| GB.Running) return;
            _gbRunner = Task.Run(() => { runMainLoop(false); });
        }
        public static void Step() {
            if (GB is null || _gbRunner is not null || GB.Running) return;
            _gbRunner = Task.Run(()=> { runMainLoop(true); });
        }
        private static void runMainLoop(bool step)
        {
            //This should be ran on a separate thread
            //Call GB.MainLoop() here once. If it returns we should notify the UI thread, and return.
            Debug.WriteLine("Emulator Start");//Stepping doesn't stop the emulator, but Starting and then Stopping it does???
            MainWindow.Main?.Dispatcher.Invoke(() => { GigaboyRefresh?.Invoke(null, EventArgs.Empty); });
            try
            {
                if (GB is not null)
                {
                    if (step)
                    {
                        GB.Step();
                    }
                    else
                    {
                        GB.MainLoop();
                    }
                    MainWindow.Main?.Dispatcher.Invoke(() => { Render(); Debug.WriteLine("Frame Attempt"); });
                }
            }
            catch (Exception e)
            {
                string msg = $"Emulation Exception ({e.GetType().Name}): {e}";
                if (MainWindow.Main is not null)  //For some reason Environment.Exit doesn't instantly close the process, and the emulator continues to run in the background for a little bit, so I had to implement  null check here.
                {
                    throw e;
                    MessageBox.Show(msg, "Emulation Exception", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                }
                else
                {
                    Environment.Exit(0);
                }
                Debug.WriteLine(msg);
                GB = null;
            }
            finally {
                _gbRunner = null;
            }
            MainWindow.Main?.Dispatcher.Invoke(() => { GigaboyRefresh?.Invoke(null, EventArgs.Empty); });
            Debug.WriteLine("Emulator Exit");
        }
        public static void Stop() {
            if (GB is not null && _gbRunner is not null && GB.Running)
            {
                GB.Stop(true);
                _gbRunner = null;
            }
            Render();
        }
        private static void OnFrame(object? sender,EventArgs e) {
            VisibleImage.Dispatcher.InvokeAsync(Render);
        }

        public static void Render() {
            if (GB is null) {
                GBArgs = null;
                return; 
            }
            if (GBArgs is null) GBArgs = new gbEventArgs(GB);   //This if statement condition should never be true, but Visual Studio complains if I don't check GBArgs for null, so I added a fix in case GBArgs somehow is null here.

            //Debug.WriteLine("Frame Update");
            lock (GB)
            {
                DrawGB(VisibleImage, GB.PPU.GetFrame(), 0, 0);
                GBFrameReady?.Invoke(null, GBArgs);
            }
        }

        public static void DrawGB(WriteableBitmap bitmap, Span2D<ColorContainer> image, int x, int y) {
            if (bitmap.BackBufferStride < image.Width + x || bitmap.Height < image.Height + y) throw new ArgumentOutOfRangeException($"Bitmap is too small. It has to be at least {image.Width + x}x{image.Height + y}");
            bitmap.Lock();
            try
            {
                unsafe
                {
                    var stride = bitmap.BackBufferStride/sizeof(int);
                    Span2D<int> data = new Span2D<int>(new Span<int>((void*)bitmap.BackBuffer, stride * bitmap.PixelHeight), stride, bitmap.PixelHeight);
                    for (int v = 0; v < image.Height; v++)
                    {
                        for (int u = 0; u < image.Width; u++)
                        {
                            var col = image[v, u].ARGB;
                            data[v + y, u + x] = col;
                        }
                    }
                }
                bitmap.AddDirtyRect(new Int32Rect(x,y,image.Width,image.Height));
            }
            finally {
                bitmap.Unlock();
            }
        }

    }
}
