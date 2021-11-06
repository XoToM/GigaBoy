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
    /// Interaction logic for TileView.xaml
    /// </summary>
    public partial class TileView : UserControl
    {
        public TileView()
        {
            InitializeComponent();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RenderOptions.SetBitmapScalingMode(Tileset1, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(Tileset2, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(Tileset3, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(Tileset1, EdgeMode.Aliased);
            RenderOptions.SetEdgeMode(Tileset2, EdgeMode.Aliased);
            RenderOptions.SetEdgeMode(Tileset3, EdgeMode.Aliased);
        }
    }
}
