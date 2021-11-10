using GigaBoy.Components;
using GigaBoy.Components.Graphics;
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
    public enum CharacterTileDataBank { x8000,x8800,x9000 }
    /// <summary>
    /// Interaction logic for TileDisplay.xaml
    /// </summary>
    public partial class CharacterDisplay : UserControl
    {
        public WriteableBitmap tileImage = new(8,8,96,96,PixelFormats.Bgra32,null);
        public CharacterDisplay()
        {
            InitializeComponent();
        }



        public CharacterTileDataBank TileDataBank
        {
            get { return (CharacterTileDataBank) GetValue(TileDataBankProperty); }
            set { SetValue(TileDataBankProperty, value); Render(true); }
        }


        // Using a DependencyProperty as the backing store for TileDataBank.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileDataBankProperty =
            DependencyProperty.Register("TileDataBank", typeof(CharacterTileDataBank), typeof(CharacterDisplay), new PropertyMetadata(CharacterTileDataBank.x8000));


        public byte Character
        {
            get { return (byte)GetValue(CharacterProperty); }
            set { SetValue(CharacterProperty, value); Render(true); }
        }

        // Using a DependencyProperty as the backing store for Character.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CharacterProperty =
            DependencyProperty.Register("Character", typeof(byte), typeof(CharacterDisplay), new PropertyMetadata((byte)0));





        private void Emulation_GBFrameReady(object? sender, Emulation.gbEventArgs e)
        {
            Render();
        }
        public void Render(bool forced=false) {
            if (Emulation.GB is null) return;

            CRAM cram = Emulation.GB.CRAMBanks[(int)TileDataBank];

            if (forced | (cram.Modified & cram.ModifiedCharacters[Character])) {
                Span2D<ColorContainer> image = new (stackalloc ColorContainer[8*8], 8, 8);
                cram.GetCharacter(ref image, Character,Emulation.GB.PPU.Palette,PaletteType.Background);
                Emulation.DrawGB(tileImage, image, 0, 0);
                cram.ModifiedCharacters[Character] = false;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Emulation.GBFrameReady -= Emulation_GBFrameReady;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Emulation.GBFrameReady += Emulation_GBFrameReady;
            ImageBox.Source = tileImage;
            RenderOptions.SetBitmapScalingMode(ImageBox, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(ImageBox, EdgeMode.Aliased);
            Render(true);
        }
    }
}
