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
        

        private void ImageBox_Loaded(object sender, RoutedEventArgs e)
        {
            Emulation.GBFrameReady += Emulation_GBFrameReady;
            RenderOptions.SetBitmapScalingMode(ImageBox,BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(ImageBox,EdgeMode.Aliased);
            Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\blargg_test_roms\dmg_cpu_instrs.gb");//ROM never halts, and since the PPU is currently broken its impossible to tell whetever the emulator passes the tests or not.

            //Emulation.Init(Environment.CurrentDirectory + @"\GigaBoyTests\mooneye_test_roms\boot_div-dmg0.gb");//Currently broken, as it executes a broken jump instruction. Usually this would result in an error, but in this case the jump instruction creates an infinite loop.
            //ToDo: Either implement the STOP and HALT instructions, and possibly add a button which forces the program to throw an error, which will also pring the debug backlog.
            //Emulation.Start();
        }

        private void Emulation_GBFrameReady(object? sender, Emulation.gbEventArgs e)
        {
            ImageBox.Source = Emulation.VisibleImage;
        }

        private void ImageBox_Unloaded(object sender, RoutedEventArgs e)
        {

            Emulation.GBFrameReady -= Emulation_GBFrameReady;
        }
    }
}
