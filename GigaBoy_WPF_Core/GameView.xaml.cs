﻿using System;
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

namespace GigaBoy_WPF.Components
{
	/// <summary>
	/// Interaction logic for GameView.xaml
	/// </summary>
	public partial class GameView : UserControl
	{
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
			RenderOptions.SetBitmapScalingMode(ImageBox, BitmapScalingMode.NearestNeighbor);
			RenderOptions.SetEdgeMode(ImageBox, EdgeMode.Aliased);

			//Emulation.Init(@"C:\Users\xotom\Desktop\asm\gb\Tetris.gb");			//  ROM reads LY, which contains an invalid value which never changes (PPU off?). This causes the rom to lock up
			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\my_test_roms\testRom.gb");
			//Emulation.Init(@"C:\Users\xotom\Desktop\asm\gb\Pokemon Red.gb");		//  ROM Mapper not implemented yet

			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\blargg_test_roms\dmg_cpu_instrs.gb");	//ROM never halts, and since the PPU is currently broken its impossible to tell whetever the emulator passes the tests or not.
			Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\dmg_acid2\dmg-acid2.gb");	//ROM never halts, and since the PPU is currently broken its impossible to tell whetever the emulator passes the tests or not.
			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\my_test_roms\cpu_test.gb");
			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\my_test_roms\squares.gb");

			//Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\mooneye_test_roms\boot_div-dmg0.gb");//Currently broken, as it executes a broken jump instruction. Usually this would result in an error, but in this case the jump instruction creates an infinite loop.
			//ToDo: Implement the HALT instruction.
			//Emulation.GB?.AddBreakpoint(0x02B7,new GigaBoy.BreakpointInfo() { BreakOnExecute=true,BreakOnJump=true,BreakOnRead=true });
			Emulation.GB.BreakpointsEnable = false;
			Emulation.Start();

		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			Emulation.GBFrameReady -= Emulation_GBFrameReady;
		}
	}
}