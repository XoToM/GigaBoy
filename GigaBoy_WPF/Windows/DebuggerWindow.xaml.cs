using GigaBoy;
using GigaBoy_WPF.Components;
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
using WpfHexaEditor.Core;

namespace GigaBoy_WPF.Windows
{
    /// <summary>
    /// Interaction logic for DebuggerWindow.xaml
    /// </summary>
    public partial class DebuggerWindow : Window
    {
        public ICommand EmulatorControlCommand => Emulation.EmulatorControlCommand;
        public CustomBackgroundBlock PCIndicator = new(0x100,1,Brushes.Aquamarine,"Program Counter");
        public DebuggerWindow()
        {
            Emulation.GigaboyRefresh += Emulation_GigaboyRefresh;
            Emulation.GBFrameReady += Emulation_GBFrameReady;
            InitializeComponent();
        }
        public void RefreshBytes() {
            if (Emulation.GB is not null)
            {
                HexViewerMain.RefreshView();
            }
        }
        public int frameCounter = 10;
        private ushort lastPC = 0x100;
        private void Emulation_GBFrameReady(object? sender, Emulation.GbEventArgs e)
        {
            var pc = Emulation.GB?.CPU.PC;
            if (pc.HasValue)
            {
                HexViewerMain.RemoveHighLight(lastPC, 1);
                HexViewerMain.AddHighLight(pc.Value, 1);
                lastPC = pc.Value;
            }
            if (frameCounter-- <= 0) {
                frameCounter = 10;
                
                RefreshBytes();
            }
        }
        private GBInstance? lastGB = null;
        private void Emulation_GigaboyRefresh(object? sender, EventArgs e)
        {
            if (lastGB != Emulation.GB) {

                HexViewerMain.RemoveHighLight(lastPC, 1);
                lastPC = 0x100;
                HexViewerMain.AddHighLight(0x100, 1); 
                if (Emulation.GB is not null)
                {
                    HexViewerMain.Stream = (System.IO.Stream)Emulation.GB.MemoryMapper;
                }
                else
                {
                    HexViewerMain.Stream = null;
                }
            }
            lastGB = Emulation.GB;
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //HexViewerMain.CustomBackgroundBlockItems.Add(PCIndicator);
            var breakpointItem = new MenuItem() { Header = "Add Breakpoint", Command = new CommandAction(()=> {
                if (Emulation.GB is null) return;
                lock (Emulation.GB) {
                    Emulation.GB.AddBreakpoint((ushort)HexViewerMain.SelectionStart, new() { BreakOnExecute=true,BreakOnJump=true });
                }
            }) };
            HexViewerMain.ContextMenu.Items.Add(breakpointItem);

            var pcViewItem = new MenuItem()
            {
                Header = "Go To PC",
                Command = new CommandAction(() => {
                    if (Emulation.GB is null) return;
                    lock (Emulation.GB)
                    {
                        HexViewerMain.SetPosition(Emulation.GB.CPU.PC,1);
                    }
                })
            };
            HexViewerMain.ContextMenu.Items.Add(pcViewItem);
        }
    }
}
