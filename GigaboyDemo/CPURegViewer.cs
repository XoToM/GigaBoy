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
    public partial class CPURegViewer : UserControl
    {
        public GBInstance? GB { get; set; } = null;
        public Form1 MainWindow { get; set; }
        public CPURegViewer()
        {
            InitializeComponent();
        }

        public void RefreshRegisters() {
            if (GB is null) return;
            var cpu = GB.CPU;
            aRegView.Value = cpu.A;
            bRegView.Value = cpu.B;
            cRegView.Value = cpu.C;
            dRegView.Value = cpu.D;
            eRegView.Value = cpu.E;
            hRegView.Value = cpu.H;
            lRegView.Value = cpu.L;
            pcRegView.Value = cpu.PC;
            spRegView.Value = cpu.SP;
            runningChkBx.Checked = !MainWindow.GBPaused;
        }

        private void aRegView_ValueChanged(object sender, EventArgs e)
        {
            if (GB is null) return;
            GB.CPU.A = (byte)aRegView.Value;
        }
        private void bRegView_ValueChanged(object sender, EventArgs e)
        {
            if (GB is null) return;
            GB.CPU.B = (byte)bRegView.Value;
        }
        private void cRegView_ValueChanged(object sender, EventArgs e)
        {
            if (GB is null) return;
            GB.CPU.C = (byte)cRegView.Value;
        }
        private void dRegView_ValueChanged(object sender, EventArgs e)
        {
            if (GB is null) return;
            GB.CPU.D = (byte)dRegView.Value;
        }
        private void eRegView_ValueChanged(object sender, EventArgs e)
        {
            if (GB is null) return;
            GB.CPU.E = (byte)eRegView.Value;
        }
        private void hRegView_ValueChanged(object sender, EventArgs e)
        {
            if (GB is null) return;
            GB.CPU.H = (byte)hRegView.Value;
        }
        private void lRegView_ValueChanged(object sender, EventArgs e)
        {
            if (GB is null) return;
            GB.CPU.L = (byte)lRegView.Value;
        }
        private void pcRegView_ValueChanged(object sender, EventArgs e)
        {
            if (GB is null) return;
            GB.CPU.PC = (ushort)pcRegView.Value;
        }
        private void spRegView_ValueChanged(object sender, EventArgs e)
        {
            if (GB is null) return;
            GB.CPU.SP = (ushort)spRegView.Value;
        }

        private void CPURegViewer_Load(object sender, EventArgs e)
        {
        }

        private void runningChkBx_CheckedChanged(object sender, EventArgs e)
        {
            MainWindow.GBPaused = !runningChkBx.Checked;
        }

        private void stepBtn_Click(object sender, EventArgs e)
        {
            MainWindow.StepGB();
            RefreshRegisters();
        }
    }
}
