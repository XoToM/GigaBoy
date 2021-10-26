using GigaBoy;
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
    public partial class Debugger : Form
    {
        public GBInstance GB { get; set; }
        public Debugger(GBInstance gb)
        {
            GB = gb;
            InitializeComponent();
        }

        private void Debugger_Load(object sender, EventArgs e)
        {
            hexView1.HexData = GB.MemoryMapper;
        }
    }
}
