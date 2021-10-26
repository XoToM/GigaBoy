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
    public partial class RegisterViewer : Form
    {
        public GBInstance GB { get; init; }
        public Form1 MainWindow { get; init; }
        public RegisterViewer(GBInstance gb,Form1 mainWindow)
        {
            GB = gb;
            MainWindow = mainWindow;
            InitializeComponent();
        }

        private void RegisterViewer_Load(object sender, EventArgs e)
        {
            cpuRegViewer1.GB = GB;
            cpuRegViewer1.MainWindow = MainWindow;
        }
        public void RefreshRegisters() {
            cpuRegViewer1.RefreshRegisters();
        }
    }
}
