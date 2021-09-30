
namespace GigaboyDemo
{
    partial class TilemapViewer
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
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.drawCameraBox = new System.Windows.Forms.CheckBox();
            this.tm = new GigaboyDemo.TilemapView();
            this.TileMapChooser = new System.Windows.Forms.CheckedListBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(12, 12);
            this.numericUpDown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(77, 27);
            this.numericUpDown1.TabIndex = 2;
            this.numericUpDown1.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // drawCameraBox
            // 
            this.drawCameraBox.AutoSize = true;
            this.drawCameraBox.Checked = true;
            this.drawCameraBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.drawCameraBox.Location = new System.Drawing.Point(96, 9);
            this.drawCameraBox.Name = "drawCameraBox";
            this.drawCameraBox.Size = new System.Drawing.Size(121, 24);
            this.drawCameraBox.TabIndex = 3;
            this.drawCameraBox.Text = "Draw Camera";
            this.drawCameraBox.UseVisualStyleBackColor = true;
            this.drawCameraBox.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // tm
            // 
            this.tm.DrawScreen = true;
            this.tm.GB = null;
            this.tm.Location = new System.Drawing.Point(13, 46);
            this.tm.MinimumSize = new System.Drawing.Size(256, 256);
            this.tm.Name = "tm";
            this.tm.Scaling = 2;
            this.tm.Size = new System.Drawing.Size(512, 512);
            this.tm.TabIndex = 4;
            this.tm.TileData = ((ushort)(32768));
            this.tm.TileMap = ((ushort)(38912));
            // 
            // TileMapChooser
            // 
            this.TileMapChooser.CheckOnClick = true;
            this.TileMapChooser.ColumnWidth = 80;
            this.TileMapChooser.FormattingEnabled = true;
            this.TileMapChooser.Items.AddRange(new object[] {
            "0x9800",
            "0x9C00"});
            this.TileMapChooser.Location = new System.Drawing.Point(223, 9);
            this.TileMapChooser.MultiColumn = true;
            this.TileMapChooser.Name = "TileMapChooser";
            this.TileMapChooser.Size = new System.Drawing.Size(170, 26);
            this.TileMapChooser.TabIndex = 1;
            this.TileMapChooser.SelectedValueChanged += new System.EventHandler(this.TileMapChooser_SelectedValueChanged);
            // 
            // TilemapViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(543, 564);
            this.Controls.Add(this.TileMapChooser);
            this.Controls.Add(this.tm);
            this.Controls.Add(this.drawCameraBox);
            this.Controls.Add(this.numericUpDown1);
            this.Name = "TilemapViewer";
            this.Text = "VRAMViewer";
            this.Load += new System.EventHandler(this.TilemapViewer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.CheckBox drawCameraBox;
        public TilemapView tm;
        private System.Windows.Forms.CheckedListBox TileMapChooser;
    }
}