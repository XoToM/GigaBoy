
namespace GigaboyDemo
{
    partial class TileDataViewer
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
            this.tv = new GigaboyDemo.TileDataView();
            this.SuspendLayout();
            // 
            // tv
            // 
            this.tv.DisplayTileDataSections = 0;
            this.tv.DisplayTileDataSectionsCount = 3;
            this.tv.Location = new System.Drawing.Point(13, 13);
            this.tv.Name = "tv";
            this.tv.Scaling = 3;
            this.tv.Size = new System.Drawing.Size(480, 576);
            this.tv.TabIndex = 0;
            // 
            // TileDataViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tv);
            this.Name = "TileDataViewer";
            this.Text = "TileDataViewer";
            this.Load += new System.EventHandler(this.TileDataViewer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        public TileDataView tv;
    }
}