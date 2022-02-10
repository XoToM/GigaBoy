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
    public enum TileViewMode { TileRange,Tilemap, Manual }
    /// <summary>
    /// Interaction logic for TileView.xaml
    /// </summary>
    public partial class TileView : UserControl
    {
        public CharacterTileDataBank TileDataBank
        {
            get { return (CharacterTileDataBank)GetValue(TileDataBankProperty); }
            set { SetValue(TileDataBankProperty, value); UpdateTileView(); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileDataBankProperty =
            DependencyProperty.Register("TileDataBank", typeof(CharacterTileDataBank), typeof(TileView), new PropertyMetadata(CharacterTileDataBank.x8000));



        public int TileIndexStart
        {
            get { return (int)GetValue(TileIndexStartProperty); }
            set { SetValue(TileIndexStartProperty, value); UpdateTileView(); }
        }

        // Using a DependencyProperty as the backing store for TileIndexStart.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileIndexStartProperty =
            DependencyProperty.Register("TileIndexStart", typeof(int), typeof(TileView), new PropertyMetadata(0));



        public int TileIndexCount
        {
            get { return (int)GetValue(TileIndexCountProperty); }
            set { SetValue(TileIndexCountProperty, value); UpdateTileView(); }
        }

        // Using a DependencyProperty as the backing store for TileIndexCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileIndexCountProperty =
            DependencyProperty.Register("TileIndexCount", typeof(int), typeof(TileView), new PropertyMetadata(0));


        public TileViewMode TileViewMode
        {
            get { return (TileViewMode)GetValue(TileViewModeProperty); }
            set { SetValue(TileViewModeProperty, value); UpdateTileView(); }
        }

        // Using a DependencyProperty as the backing store for TileViewMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileViewModeProperty =
            DependencyProperty.Register("TileViewMode", typeof(TileViewMode), typeof(TileView), new PropertyMetadata(TileViewMode.TileRange));


        public TilemapBank TilemapBank
        {
            get { return (TilemapBank)GetValue(TilemapBankProperty); }
            set { SetValue(TilemapBankProperty, value); UpdateTileView(); }
        }

        // Using a DependencyProperty as the backing store for TilemapBank.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TilemapBankProperty =
            DependencyProperty.Register("TilemapBank", typeof(TilemapBank), typeof(TileView), new PropertyMetadata(TilemapBank.x9800));



        public Thickness Spacing
        {
            get { return (Thickness)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Spacing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register("Spacing", typeof(Thickness), typeof(TileView), new PropertyMetadata(new Thickness(-4,-1,-4,-1)));



        public double TileSize
        {
            get { return (double)GetValue(TileSizeProperty); }
            set { SetValue(TileSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TileSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileSizeProperty =
            DependencyProperty.Register("TileSize", typeof(double), typeof(TileView), new PropertyMetadata(24.0));

        public double ViewWidth
        {
            get { return (double)GetValue(ViewWidthProperty); }
            set { SetValue(ViewWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TileSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewWidthProperty =
            DependencyProperty.Register("ViewWidth", typeof(double), typeof(TileView), new PropertyMetadata(1.0));
        public double ViewHeight
        {
            get { return (double)GetValue(ViewHeightProperty); }
            set { SetValue(ViewHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TileSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewHeightProperty =
            DependencyProperty.Register("ViewHeight", typeof(double), typeof(TileView), new PropertyMetadata(1.0));

        public Visibility ViewportVisibility
        {
            get { return (Visibility)GetValue(ViewportVisibilityProperty); }
            set { SetValue(ViewportVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TileSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewportVisibilityProperty =
            DependencyProperty.Register("ViewportVisibility", typeof(Visibility), typeof(TileView), new PropertyMetadata(Visibility.Collapsed));




        public TileView()
        {
            InitializeComponent();
        }
        public void Recount() {
            List<byte> items = new List<byte>();
            for (int i = 0; i < TileIndexCount; i++) {
                items.Add((byte)((i+TileIndexStart)%128));
            }
            ItemDisplayList.ItemsSource = items;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTileView();
            Emulation.GBFrameReady += Emulation_GBFrameReady;
            ViewWidth = TileSize * 20;
            ViewHeight = TileSize * 18;
        }

        private void Emulation_GBFrameReady(object? sender, Emulation.GbEventArgs e)
        {
            if (TileViewMode == TileViewMode.Tilemap && Emulation.GB is not null && Emulation.GB.TMRAMBanks[(int)TilemapBank].Modified) RedrawTilemap();
        }

        public void UpdateTileView() {
            switch (TileViewMode)
            {
                case TileViewMode.TileRange:
                    Recount();
                    break;
                case TileViewMode.Tilemap:
                    RedrawTilemap();
                    break;
                case TileViewMode.Manual:
                    return;
            }
        }
        public void RedrawTilemap() {
            //System.Diagnostics.Debug.WriteLine($"Redrawing Tilemap {TilemapBank} with tileset {TileDataBank}");
            if (Emulation.GB is null) return;
            var gb = Emulation.GB;
            ViewportVisibility = Visibility.Visible;
            ViewWidth = TileSize * 20;
            ViewHeight = TileSize * 18;
            var tmram = gb.TMRAMBanks[(int)TilemapBank];
            byte[] tilemap = new byte[32 * 32];
            tmram.Memory.CopyTo<byte>(tilemap.AsSpan());
            ItemDisplayList.ItemsSource = tilemap;

            var pxl = (TileSize / 8);

            Canvas.SetLeft(v1, gb.PPU.SCX * pxl);
            Canvas.SetTop(v1, gb.PPU.SCY * pxl);

            Canvas.SetLeft(v2, (gb.PPU.SCX - 32*8) * pxl);
            Canvas.SetTop(v2, (gb.PPU.SCY) * pxl);

            Canvas.SetLeft(v3, (gb.PPU.SCX) * pxl);
            Canvas.SetTop(v3, (gb.PPU.SCY - 32 * 8) * pxl);

            Canvas.SetLeft(v4, (gb.PPU.SCX - 32 * 8) * pxl);
            Canvas.SetTop(v4, (gb.PPU.SCY - 32 * 8) * pxl);
        }
    }
}
