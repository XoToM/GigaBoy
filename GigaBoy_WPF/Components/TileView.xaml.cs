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
        public CharacterTileDataBank TileDataBank
        {
            get { return (CharacterTileDataBank)GetValue(TileDataBankProperty); }
            set { SetValue(TileDataBankProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileDataBankProperty =
            DependencyProperty.Register("TileDataBank", typeof(CharacterTileDataBank), typeof(TileView), new PropertyMetadata(CharacterTileDataBank.x8000));

        public TileView()
        {
            InitializeComponent();
        }

    }
}
