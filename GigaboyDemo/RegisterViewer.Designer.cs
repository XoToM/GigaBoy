
namespace GigaboyDemo
{
    partial class RegisterViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cpuRegViewer1 = new GigaboyDemo.CPURegViewer();
            this.SuspendLayout();
            // 
            // cpuRegViewer1
            // 
            this.cpuRegViewer1.GB = null;
            this.cpuRegViewer1.Location = new System.Drawing.Point(13, 13);
            this.cpuRegViewer1.Name = "cpuRegViewer1";
            this.cpuRegViewer1.Size = new System.Drawing.Size(450, 429);
            this.cpuRegViewer1.TabIndex = 0;
            // 
            // RegisterViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.cpuRegViewer1);
            this.Name = "RegisterViewer";
            this.Text = "RegisterViewer";
            this.Load += new System.EventHandler(this.RegisterViewer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private CPURegViewer cpuRegViewer1;
    }
}