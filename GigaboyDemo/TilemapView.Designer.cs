
namespace GigaboyDemo
{
    partial class TilemapView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TilemapView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.MinimumSize = new System.Drawing.Size(256, 256);
            this.Name = "TilemapView";
            this.Size = new System.Drawing.Size(360, 354);
            this.Load += new System.EventHandler(this.TilemapViewer_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.TilemapViewer_Paint);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
