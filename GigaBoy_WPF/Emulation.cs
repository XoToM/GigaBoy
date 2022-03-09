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
using System.Threading;

namespace GigaBoy_WPF
{
    public enum CharacterTileDataBank { x8000=0, x8800=1, x9000=2 }
    public enum TilemapBank { x9800, x9C00 }
    public static class Emulation       //This class connects the UI and the emulator together, and provides helper methods for things such as rendering.
    {
        public static ICommand EmulatorControlCommand { get; private set; } = new EmulatorCommandRunner();
        private static GBInstance? _gBInstance;
        private static Task? _gbRunner;
        private static bool wasInitialised = false;
        private static bool animateBoy = false;
        public static WriteableBitmap[] TileBitmaps { get; private set; } = new WriteableBitmap[128 * 3];

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
        public static GbEventArgs? GBArgs { get; private set; } = null;
        public class GbEventArgs : EventArgs {
            public GBInstance GB { get; protected internal set; }
            public GbEventArgs(GBInstance gb) {
                GB = gb;
            }
        }

        public static string? RomFilePath { get; private set; }
        public static WriteableBitmap VisibleImage { get; private set; } = new(160, 144, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);


        public static event EventHandler? GigaboyRefresh;
        /// <summary>
        /// This event is invoked when the emulator finishes drawing a frame. It is called from the UI thread, and it locks the GBInstance object, so all of its members can be safely accessed inside the event handler.
        /// </summary>
        public static event EventHandler<GbEventArgs>? GBFrameReady;

        private static void Prepare() {
            if (wasInitialised) return;
            //Span2D<ColorContainer> image = new Span2D<ColorContainer>(stackalloc ColorContainer[8*8],8,8);
            //image.Buffer.Fill(new ColorContainer(255, 255, 255));

            for (int i = 0; i < TileBitmaps.Length; i++) {
                var bmp = new WriteableBitmap(8, 8, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
                //DrawGB(bmp,image,0,0);
                TileBitmaps[i] = bmp;
            }
            wasInitialised = true;
        }
        public static void Restart(string? romPath) {
            Stop();
            Init(romPath);


            //GB?.AddBreakpoint(0xFF4D, new() { BreakOnRead = true });

            Start();
        }
        public static void Init(string? romPath) {
            Prepare();
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
            //GB.PPU.Debug = true;
            //GB.CPU.PrintOperation = true;   //Warning: Setting this to true while BacklogOnlyLogging is set to false will defenestrate performance. Enable at your own risk!
            GB.DebugLogging = true;   //Warning: Setting this to true might defenestrate performance. Enable at your own risk!
            GB.BacklogOnlyLogging = false;
            GB.PPU.FrameRendered += OnFrame;
            GB.BreakpointsEnable = true;
            GB.PPU.SpriteDebugLines = true;
            //GB.SpeedMultiplier = 5000;
            //GB.FrameAutoRefreshTreshold = double.MaxValue;
            Render(true);
        }

        private static void GB_Breakpoint(object? sender, EventArgs e)
        {
            GB?.Stop();
            MainWindow.Main?.Dispatcher.InvokeAsync(Stop);
            Debug.WriteLine($"Breakpoint Hit {_gbRunner is not null}");
        }

        public static void Start() {
            animateBoy = false;
            //Prepare and start the thread which will run the mainLoop() method.
            if (GB is null || _gbRunner is not null|| GB.Running) return;
            _gbRunner = Task.Run(() => { RunMainLoop(false); });
        }
        public static void Step() {
            animateBoy = false;
            if (GB is null || _gbRunner is not null || GB.Running) return;
            _gbRunner = Task.Run(()=> { RunMainLoop(true); });
        }
        public static void Animate()
        {
            animateBoy = true;
            if (GB is null || _gbRunner is not null || GB.Running) return;
            _gbRunner = Task.Run(() => { RunMainLoop(true); });
        }
        private static void RunMainLoop(bool step)
        {
            //This should be ran on a separate thread
            //Call GB.MainLoop() here once. If it returns we should notify the UI thread, and return.
            Debug.WriteLine("Emulator Start");
            MainWindow.Main?.Dispatcher.Invoke(() => { GigaboyRefresh?.Invoke(null, EventArgs.Empty); });
            try
            {
                if (GB is not null)
                {
                    do
                    {
                        if (step)
                        {
                            GB.Step();
                            MainWindow.Main?.Dispatcher.Invoke(() => { GigaboyRefresh?.Invoke(null, EventArgs.Empty); });
                            if (animateBoy)
                            {
                                Thread.Sleep(16);
                            }
                        }
                        else
                        {
                            GB.MainLoop();
                        }
                        MainWindow.Main?.Dispatcher.Invoke(() => { Render(true); });
                    } while (animateBoy);
                }
            }
            catch (Exception e)
            {
                string msg = $"Emulation Exception ({e.GetType().Name}): {e}";
                if (MainWindow.Main is not null)  //For some reason Environment.Exit doesn't instantly close the process, and the emulator continues to run in the background for a little bit, so I had to implement null check here.
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
                return;
            }
            finally {
                _gbRunner = null;
            }
            MainWindow.Main?.Dispatcher.Invoke(() => { GigaboyRefresh?.Invoke(null, EventArgs.Empty); });
            Debug.WriteLine("Emulator Exit");
        }
        public static void Stop() {
            animateBoy = false;
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
        public static void Render() { Render(false); }

        public static void Render(bool forced) {
            if (GB is null) {
                GBArgs = null;
                return; 
            }
            if (GBArgs is null) GBArgs = new GbEventArgs(GB);   //This if statement condition should never be true, but Visual Studio complains if I don't check GBArgs for null, so I added a fix in case GBArgs somehow is null here.

            //Debug.WriteLine("Frame Update");
            lock (GB)
            {
                DrawGB(VisibleImage, GB.PPU.GetFrame(), 0, 0);
                for (int i=0;i<GB.CRAMBanks.Length*128;i++) {
                    var cram = GB.CRAMBanks[i/128];
                    RenderTile(i,forced);
                }
                GBFrameReady?.Invoke(null, GBArgs);
                foreach (var cram in GB.CRAMBanks) cram.Modified = false;
                foreach (var tmram in GB.TMRAMBanks) tmram.Modified = false;
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
                    Span2D<int> data = new(new Span<int>((void*)bitmap.BackBuffer, stride * bitmap.PixelHeight), stride, bitmap.PixelHeight);
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

        public static void RenderTile(byte tile,CharacterTileDataBank bank,bool forced = false)
        {
            if (Emulation.GB is null||tile>=128) return;


            CRAM cram = Emulation.GB.CRAMBanks[(int)bank];

            if (forced | (cram.Modified & cram.ModifiedCharacters[tile]))
            {
                var tileImage = TileBitmaps[tile + 128 * (int)bank];

                Span2D<ColorContainer> image = new(stackalloc ColorContainer[8 * 8], 8, 8);
                cram.GetCharacter(ref image, tile, Emulation.GB.PPU.Palette, PaletteType.Background);
                Emulation.DrawGB(tileImage, image, 0, 0);
                cram.ModifiedCharacters[tile] = false;
            }
        }
        public static void RenderTile(int i, bool forced = false) {
            RenderTile((byte)(i%128),(CharacterTileDataBank)(i/128),forced);
        }
        public static WriteableBitmap GetTileBitmap(byte tile, CharacterTileDataBank bank) {
            if (tile > 127)
            {
                return TileBitmaps[tile];
            }
            return TileBitmaps[tile+(int)bank*128];
        }

    }
}