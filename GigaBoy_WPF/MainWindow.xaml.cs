using GigaBoy_WPF.Windows;
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

namespace GigaBoy_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow? Main { get; private set; }
        public static DebuggerWindow? Debugger { get; private set; }
        public static VRAM_TileViewer? TileDataViewer { get; private set; }
        public static VRAM_TileMapViewer? TileMapViewer { get; private set; }
        public MainWindow()
        {
            Main = this;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Debugger is null)
            {
                Debugger = new();
                Debugger.Closing += Debugger_Closing;
                Debugger.Show();
            }
            if (TileDataViewer is null)
            {
                TileDataViewer = new();
                TileDataViewer.Closing += TileDataViewer_Closing;
                TileDataViewer.Show();
            }
            if (TileMapViewer is null)
            {
                TileMapViewer = new();
                TileMapViewer.Closing += TileMapViewer_Closing;
                TileMapViewer.Show();
            }
        }

        private void TileMapViewer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (TileMapViewer is null) return;
            TileMapViewer.Closing -= TileMapViewer_Closing;
            TileMapViewer = null;
        }

        private void TileDataViewer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (TileDataViewer is null) return;
            TileDataViewer.Closing -= TileDataViewer_Closing;
            TileDataViewer = null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Main = null;   
            if (Debugger is not null)
            {
                Debugger.Close();
            }
            Application.Current.Shutdown(0);
            //Appclication.Shutdown should close the program, but in case it doesnt I added Environment.Exit here as well.
            Environment.Exit(0);
        }
        private void Debugger_Closing(object sender,System.ComponentModel.CancelEventArgs e) {
            if (Debugger is null) return;
            Debugger.Closing -= Debugger_Closing;
            Debugger = null;
        }
    }
}
