using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GigaboyDemo
{
    public partial class TilemapViewer : Form
    {
        public TilemapViewer()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            tm.DrawScreen = drawCameraBox.Checked;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            tm.Scaling = (int)numericUpDown1.Value;
        }

        private void TilemapViewer_Load(object sender, EventArgs e)
        {

        }

        private void TileMapChooser_SelectedValueChanged(object sender, EventArgs e)
        {
            tm.TileMap = (ushort)((TileMapChooser.SelectedIndex==0)?0x9800:0x9C00);
        }
    }
}
