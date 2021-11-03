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
            ImageBox.Source = Emulation.VisibleImage;
            RenderOptions.SetBitmapScalingMode(ImageBox,BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(ImageBox,EdgeMode.Aliased);
            //dmg_cpu_instrs rom reads from address 0xFF4D which is the CGB speed switch register. DMG gameboy returns 0xFF when this address is read, but currently this emulator returns an undefined value, which fools the rom into beliving its running on CGB hardware. The rom then tries to execute a speed switch, which crashes the emulator, as the stop instruction has not been implemented yet.
            Emulation.Restart(Environment.CurrentDirectory + @"\GigaBoyTests\blargg_test_roms\dmg_cpu_instrs.gb");
            

            //Emulation.Restart(Environment.CurrentDirectory + @"\GigaBoyTests\mooneye_test_roms\boot_div-dmg0.gb");//Currently broken, as it executes a broken jump instruction. Usually this would result in an error, but in this case the jump instruction creates an infinite loop.
            //ToDo: Either implement the STOP and HALT instructions, and possibly add a button which forces the program to throw an error, which will also pring the debug backlog.
        }
    }
}
