using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GigaBoy_WPF_Core;
using GigaBoy.Components;

namespace GigaBoy_WPF.Components
{
	/// <summary>
	/// Interaction logic for GameView.xaml
	/// </summary>
	public partial class GameView : UserControl
	{
		public Dictionary<Key, GameboyInput> ButtonMap = new();
		public GameView()
		{
			InitializeComponent();
		}

		private void Emulation_GBFrameReady(object? sender, Emulation.GbEventArgs e)
		{
			Dispatcher.InvokeAsync(() => {
				Emulation.DrawGB(Emulation.VisibleImage, e.GB.PPU.GetFrame(), 0, 0);
				ImageBox.Source = Emulation.VisibleImage;
			});
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			Emulation.GBFrameReady += Emulation_GBFrameReady;

			ButtonMap[Key.Up] = GameboyInput.Up;
			ButtonMap[Key.Down] = GameboyInput.Down;
			ButtonMap[Key.Left] = GameboyInput.Left;
			ButtonMap[Key.Right] = GameboyInput.Right;
			ButtonMap[Key.Z] = GameboyInput.A;
			ButtonMap[Key.X] = GameboyInput.B;
			ButtonMap[Key.LeftShift] = GameboyInput.Select;
			ButtonMap[Key.Space] = GameboyInput.Start;

			RenderOptions.SetBitmapScalingMode(ImageBox, BitmapScalingMode.NearestNeighbor);
			RenderOptions.SetEdgeMode(ImageBox, EdgeMode.Aliased);

			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\my_test_roms\testRom.gb");

			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\blargg_test_roms\dmg_cpu_instrs.gb");	//ROM never halts, and since the PPU is currently broken its impossible to tell whetever the emulator passes the tests or not.
			Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\dmg_acid2\dmg-acid2.gb");	//ROM never halts, and since the PPU is currently broken its impossible to tell whetever the emulator passes the tests or not.
			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\my_test_roms\cpu_test.gb");
			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\my_test_roms\squares.gb");

			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\mooneye_test_roms\boot_div-dmg0.gb");//Currently broken, as it executes a broken jump instruction. Usually this would result in an error, but in this case the jump instruction creates an infinite loop.
			//Emulation.GB?.AddBreakpoint(0x02B7,new GigaBoy.BreakpointInfo() { BreakOnExecute = true, BreakOnJump = true, BreakOnRead = true });
			
			if(Emulation.GB is null) throw new ApplicationException();
			
			Emulation.GB.BreakpointsEnable = false;
			Emulation.GB.DebugLogging = true;
			Emulation.Start();

		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			Emulation.GBFrameReady -= Emulation_GBFrameReady;
		}

		private void UserControl_Drop(object sender, DragEventArgs e)
		{
			var file = (string[])e.Data.GetData(DataFormats.FileDrop, true);
			if (file is null || file.Length < 1) return;
			if (!System.IO.File.Exists(file[0])) return;
			Emulation.Stop();
			Emulation.Init(file[0]);
			Emulation.Start();
		}
		private void UserControl_KeyDown(object sender, KeyEventArgs e)
		{
			if (ButtonMap.ContainsKey(e.Key)) Emulation.GB?.Joypad.SetButton(ButtonMap.GetValueOrDefault(e.Key), true);
		}

		private void UserControl_KeyUp(object sender, KeyEventArgs e)
		{
			if (ButtonMap.ContainsKey(e.Key)) Emulation.GB?.Joypad.SetButton(ButtonMap.GetValueOrDefault(e.Key), false);
		}

		private void UserControl_LostFocus(object sender, RoutedEventArgs e)
		{
			Emulation.GB?.Joypad.SetButton(GameboyInput.Up, false);
			Emulation.GB?.Joypad.SetButton(GameboyInput.Down, false);
			Emulation.GB?.Joypad.SetButton(GameboyInput.Left, false);
			Emulation.GB?.Joypad.SetButton(GameboyInput.Right, false);
			Emulation.GB?.Joypad.SetButton(GameboyInput.Select, false);
			Emulation.GB?.Joypad.SetButton(GameboyInput.Start, false);
			Emulation.GB?.Joypad.SetButton(GameboyInput.B, false);
			Emulation.GB?.Joypad.SetButton(GameboyInput.A, false);
		}
	}
}
