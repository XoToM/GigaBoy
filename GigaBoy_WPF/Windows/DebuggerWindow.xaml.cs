using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace GigaBoy_WPF.Windows
{
    /// <summary>
    /// Interaction logic for DebuggerWindow.xaml
    /// </summary>
    public partial class DebuggerWindow : Window
    {
        public ICommand EmulatorControlCommand => Emulation.EmulatorControlCommand;
        public DebuggerWindow()
        {
            Emulation.GigaboyRefresh += Emulation_GigaboyRefresh;
            Emulation.GBFrameReady += Emulation_GBFrameReady;
            InitializeComponent();
        }
        public void RefreshBytes() {
            if (Emulation.GB is not null)
            {
                //Debug.WriteLine("Frame Draw Event");
                HexViewerMain.RefreshView();
            }
        }
        public int frameCounter = 10;
        private void Emulation_GBFrameReady(object? sender, Emulation.gbEventArgs e)
        {
            if (frameCounter-- <= 0) {
                frameCounter = 10;
                var pc = Emulation.GB?.CPU.PC;
                if (pc.HasValue) {
                    HexViewerMain.SelectionStart = pc.Value;
                    HexViewerMain.SelectionStop = pc.Value;
                }
                RefreshBytes();
            }
        }

        private void Emulation_GigaboyRefresh(object? sender, EventArgs e)
        {
            if (Emulation.GB is not null)
            {
                HexViewerMain.Stream = (System.IO.Stream)Emulation.GB.MemoryMapper;
            }
            else {
                HexViewerMain.Stream = null;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
