
namespace GigaboyDemo
{
    partial class Debugger
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
            this.hexView1 = new GigaboyDemo.HexView();
            this.SuspendLayout();
            // 
            // hexView1
            // 
            this.hexView1.BytesPerAddress = 2;
            this.hexView1.BytesPerLine = 16;
            this.hexView1.HexData = null;
            this.hexView1.Location = new System.Drawing.Point(13, 13);
            this.hexView1.Name = "hexView1";
            this.hexView1.Size = new System.Drawing.Size(775, 425);
            this.hexView1.TabIndex = 0;
            // 
            // Debugger
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.hexView1);
            this.Name = "Debugger";
            this.Text = "Debugger";
            this.Load += new System.EventHandler(this.Debugger_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private HexView hexView1;
    }
}