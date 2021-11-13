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
using System.Windows.Shapes;

namespace GigaBoy_WPF.Windows
{
    /// <summary>
    /// Interaction logic for VRAM_TileMapViewer.xaml
    /// </summary>
    public partial class VRAM_TileMapViewer : Window
    {
        public VRAM_TileMapViewer()
        {
            InitializeComponent();
        }

        private void TileDataBankSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TileViewer.UpdateTileView();
        }

        private void TilemapBankSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TileViewer.UpdateTileView();
        }
    }
}
