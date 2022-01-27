using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using GigaBoy;
using GigaBoy.Components.Graphics;
using GigaBoy.Components;
using System.Windows;

namespace GigaBoy_WPF_Core
{
	public static class Emulation
	{
		public static GBInstance? GB { get; private set; }
		static string currentRom = String.Empty;
		public static WriteableBitmap VisibleImage { get; private set; } = new(160, 144, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
		static CancellationTokenSource? GBStopToken;

		public static event EventHandler<GbEventArgs>? GBFrameReady;


		public static GbEventArgs? GBArgs { get; private set; } = null;
		public class GbEventArgs : EventArgs
		{
			public GBInstance GB { get; protected internal set; }
			public GbEventArgs(GBInstance gb)
			{
				GB = gb;
			}
		}

		public static void Init(string rom) {
			Stop();
			currentRom = rom;
			GB = new (rom);
			GBStopToken = new ();

			GB.DebugLogging = false;
			GB.BacklogOnlyLogging = false;

            GB.PPU.FrameRendered += PPU_FrameRendered;
		}

        private static void PPU_FrameRendered(object? sender, EventArgs e)
        {
			Debug.WriteLine("Drawing Frame");
			if (GB is null) return;
			GBFrameReady?.Invoke(sender,new (GB));
        }
		public static void DrawGB(WriteableBitmap bitmap, Span2D<ColorContainer> image, int x, int y)
		{
			if (bitmap.BackBufferStride < image.Width + x || bitmap.Height < image.Height + y) throw new ArgumentOutOfRangeException($"Bitmap is too small. It has to be at least {image.Width + x}x{image.Height + y}");
			bitmap.Lock();
			try
			{
				unsafe
				{
					var stride = bitmap.BackBufferStride / sizeof(int);
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
				bitmap.AddDirtyRect(new Int32Rect(x, y, image.Width, image.Height));
			}
			finally
			{
				bitmap.Unlock();
			}
		}
		public static void Start() {
			if (GB is not null)
			{
				Runner();
			}
		}
		static async void Runner() {
			if (GB is null) return;
			Task.Run(()=> { Debug.WriteLine("Emulation Started!"); GB.MainLoop(false); Debug.WriteLine("Emulation Ended!"); });
		}
		public static void Stop() {
			if (GB is not null)
			{
				GB.Stop();
			}
		}
		public static void Restart() {
			Init(currentRom);
		}
	}
}
