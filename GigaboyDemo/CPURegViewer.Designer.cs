
namespace GigaboyDemo
{
    partial class CPURegViewer
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
            this.aRegView = new System.Windows.Forms.NumericUpDown();
            this.aRegLabel = new System.Windows.Forms.Label();
            this.bRegLabel = new System.Windows.Forms.Label();
            this.bRegView = new System.Windows.Forms.NumericUpDown();
            this.cRegLabel = new System.Windows.Forms.Label();
            this.cRegView = new System.Windows.Forms.NumericUpDown();
            this.dRegLabel = new System.Windows.Forms.Label();
            this.dRegView = new System.Windows.Forms.NumericUpDown();
            this.eRegLabel = new System.Windows.Forms.Label();
            this.eRegView = new System.Windows.Forms.NumericUpDown();
            this.hRegLabel = new System.Windows.Forms.Label();
            this.hRegView = new System.Windows.Forms.NumericUpDown();
            this.lRegLabel = new System.Windows.Forms.Label();
            this.lRegView = new System.Windows.Forms.NumericUpDown();
            this.pcRegLabel = new System.Windows.Forms.Label();
            this.pcRegView = new System.Windows.Forms.NumericUpDown();
            this.spRegLabel = new System.Windows.Forms.Label();
            this.spRegView = new System.Windows.Forms.NumericUpDown();
            this.runningChkBx = new System.Windows.Forms.CheckBox();
            this.stepBtn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.aRegView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bRegView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cRegView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dRegView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.eRegView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.hRegView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lRegView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcRegView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.spRegView)).BeginInit();
            this.SuspendLayout();
            // 
            // aRegView
            // 
            this.aRegView.Hexadecimal = true;
            this.aRegView.Location = new System.Drawing.Point(29, 4);
            this.aRegView.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.aRegView.Name = "aRegView";
            this.aRegView.Size = new System.Drawing.Size(83, 27);
            this.aRegView.TabIndex = 0;
            this.aRegView.ValueChanged += new System.EventHandler(this.aRegView_ValueChanged);
            // 
            // aRegLabel
            // 
            this.aRegLabel.AutoSize = true;
            this.aRegLabel.Location = new System.Drawing.Point(4, 11);
            this.aRegLabel.Name = "aRegLabel";
            this.aRegLabel.Size = new System.Drawing.Size(19, 20);
            this.aRegLabel.TabIndex = 1;
            this.aRegLabel.Text = "A";
            // 
            // bRegLabel
            // 
            this.bRegLabel.AutoSize = true;
            this.bRegLabel.Location = new System.Drawing.Point(4, 44);
            this.bRegLabel.Name = "bRegLabel";
            this.bRegLabel.Size = new System.Drawing.Size(18, 20);
            this.bRegLabel.TabIndex = 3;
            this.bRegLabel.Text = "B";
            // 
            // bRegView
            // 
            this.bRegView.Hexadecimal = true;
            this.bRegView.Location = new System.Drawing.Point(29, 37);
            this.bRegView.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.bRegView.Name = "bRegView";
            this.bRegView.Size = new System.Drawing.Size(83, 27);
            this.bRegView.TabIndex = 2;
            this.bRegView.ValueChanged += new System.EventHandler(this.bRegView_ValueChanged);
            // 
            // cRegLabel
            // 
            this.cRegLabel.AutoSize = true;
            this.cRegLabel.Location = new System.Drawing.Point(124, 44);
            this.cRegLabel.Name = "cRegLabel";
            this.cRegLabel.Size = new System.Drawing.Size(18, 20);
            this.cRegLabel.TabIndex = 5;
            this.cRegLabel.Text = "C";
            // 
            // cRegView
            // 
            this.cRegView.Hexadecimal = true;
            this.cRegView.Location = new System.Drawing.Point(149, 37);
            this.cRegView.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.cRegView.Name = "cRegView";
            this.cRegView.Size = new System.Drawing.Size(83, 27);
            this.cRegView.TabIndex = 4;
            this.cRegView.ValueChanged += new System.EventHandler(this.cRegView_ValueChanged);
            // 
            // dRegLabel
            // 
            this.dRegLabel.AutoSize = true;
            this.dRegLabel.Location = new System.Drawing.Point(4, 77);
            this.dRegLabel.Name = "dRegLabel";
            this.dRegLabel.Size = new System.Drawing.Size(20, 20);
            this.dRegLabel.TabIndex = 7;
            this.dRegLabel.Text = "D";
            // 
            // dRegView
            // 
            this.dRegView.Hexadecimal = true;
            this.dRegView.Location = new System.Drawing.Point(29, 70);
            this.dRegView.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.dRegView.Name = "dRegView";
            this.dRegView.Size = new System.Drawing.Size(83, 27);
            this.dRegView.TabIndex = 6;
            this.dRegView.ValueChanged += new System.EventHandler(this.dRegView_ValueChanged);
            // 
            // eRegLabel
            // 
            this.eRegLabel.AutoSize = true;
            this.eRegLabel.Location = new System.Drawing.Point(124, 77);
            this.eRegLabel.Name = "eRegLabel";
            this.eRegLabel.Size = new System.Drawing.Size(17, 20);
            this.eRegLabel.TabIndex = 9;
            this.eRegLabel.Text = "E";
            // 
            // eRegView
            // 
            this.eRegView.Hexadecimal = true;
            this.eRegView.Location = new System.Drawing.Point(149, 70);
            this.eRegView.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.eRegView.Name = "eRegView";
            this.eRegView.Size = new System.Drawing.Size(83, 27);
            this.eRegView.TabIndex = 8;
            this.eRegView.ValueChanged += new System.EventHandler(this.eRegView_ValueChanged);
            // 
            // hRegLabel
            // 
            this.hRegLabel.AutoSize = true;
            this.hRegLabel.Location = new System.Drawing.Point(4, 110);
            this.hRegLabel.Name = "hRegLabel";
            this.hRegLabel.Size = new System.Drawing.Size(20, 20);
            this.hRegLabel.TabIndex = 11;
            this.hRegLabel.Text = "H";
            // 
            // hRegView
            // 
            this.hRegView.Hexadecimal = true;
            this.hRegView.Location = new System.Drawing.Point(29, 103);
            this.hRegView.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.hRegView.Name = "hRegView";
            this.hRegView.Size = new System.Drawing.Size(83, 27);
            this.hRegView.TabIndex = 10;
            this.hRegView.ValueChanged += new System.EventHandler(this.hRegView_ValueChanged);
            // 
            // lRegLabel
            // 
            this.lRegLabel.AutoSize = true;
            this.lRegLabel.Location = new System.Drawing.Point(124, 110);
            this.lRegLabel.Name = "lRegLabel";
            this.lRegLabel.Size = new System.Drawing.Size(16, 20);
            this.lRegLabel.TabIndex = 13;
            this.lRegLabel.Text = "L";
            // 
            // lRegView
            // 
            this.lRegView.Hexadecimal = true;
            this.lRegView.Location = new System.Drawing.Point(149, 103);
            this.lRegView.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.lRegView.Name = "lRegView";
            this.lRegView.Size = new System.Drawing.Size(83, 27);
            this.lRegView.TabIndex = 12;
            this.lRegView.ValueChanged += new System.EventHandler(this.lRegView_ValueChanged);
            // 
            // pcRegLabel
            // 
            this.pcRegLabel.AutoSize = true;
            this.pcRegLabel.Location = new System.Drawing.Point(0, 166);
            this.pcRegLabel.Name = "pcRegLabel";
            this.pcRegLabel.Size = new System.Drawing.Size(26, 20);
            this.pcRegLabel.TabIndex = 15;
            this.pcRegLabel.Text = "PC";
            // 
            // pcRegView
            // 
            this.pcRegView.Hexadecimal = true;
            this.pcRegView.Location = new System.Drawing.Point(29, 164);
            this.pcRegView.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.pcRegView.Name = "pcRegView";
            this.pcRegView.Size = new System.Drawing.Size(83, 27);
            this.pcRegView.TabIndex = 14;
            this.pcRegView.ValueChanged += new System.EventHandler(this.pcRegView_ValueChanged);
            // 
            // spRegLabel
            // 
            this.spRegLabel.AutoSize = true;
            this.spRegLabel.Location = new System.Drawing.Point(124, 171);
            this.spRegLabel.Name = "spRegLabel";
            this.spRegLabel.Size = new System.Drawing.Size(25, 20);
            this.spRegLabel.TabIndex = 17;
            this.spRegLabel.Text = "SP";
            // 
            // spRegView
            // 
            this.spRegView.Hexadecimal = true;
            this.spRegView.Location = new System.Drawing.Point(149, 166);
            this.spRegView.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.spRegView.Name = "spRegView";
            this.spRegView.Size = new System.Drawing.Size(83, 27);
            this.spRegView.TabIndex = 16;
            this.spRegView.ValueChanged += new System.EventHandler(this.spRegView_ValueChanged);
            // 
            // runningChkBx
            // 
            this.runningChkBx.AutoSize = true;
            this.runningChkBx.Location = new System.Drawing.Point(11, 316);
            this.runningChkBx.Name = "runningChkBx";
            this.runningChkBx.Size = new System.Drawing.Size(85, 24);
            this.runningChkBx.TabIndex = 18;
            this.runningChkBx.Text = "Running";
            this.runningChkBx.UseVisualStyleBackColor = true;
            this.runningChkBx.CheckedChanged += new System.EventHandler(this.runningChkBx_CheckedChanged);
            // 
            // stepBtn
            // 
            this.stepBtn.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.stepBtn.Location = new System.Drawing.Point(124, 302);
            this.stepBtn.Name = "stepBtn";
            this.stepBtn.Size = new System.Drawing.Size(83, 38);
            this.stepBtn.TabIndex = 19;
            this.stepBtn.Text = "Step";
            this.stepBtn.UseVisualStyleBackColor = true;
            this.stepBtn.Click += new System.EventHandler(this.stepBtn_Click);
            // 
            // CPURegViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.stepBtn);
            this.Controls.Add(this.runningChkBx);
            this.Controls.Add(this.spRegLabel);
            this.Controls.Add(this.spRegView);
            this.Controls.Add(this.pcRegLabel);
            this.Controls.Add(this.pcRegView);
            this.Controls.Add(this.lRegLabel);
            this.Controls.Add(this.lRegView);
            this.Controls.Add(this.hRegLabel);
            this.Controls.Add(this.hRegView);
            this.Controls.Add(this.eRegLabel);
            this.Controls.Add(this.eRegView);
            this.Controls.Add(this.dRegLabel);
            this.Controls.Add(this.dRegView);
            this.Controls.Add(this.cRegLabel);
            this.Controls.Add(this.cRegView);
            this.Controls.Add(this.bRegLabel);
            this.Controls.Add(this.bRegView);
            this.Controls.Add(this.aRegLabel);
            this.Controls.Add(this.aRegView);
            this.Name = "CPURegViewer";
            this.Size = new System.Drawing.Size(360, 343);
            this.Load += new System.EventHandler(this.CPURegViewer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.aRegView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bRegView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cRegView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dRegView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.eRegView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.hRegView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lRegView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcRegView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.spRegView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown aRegView;
        private System.Windows.Forms.Label aRegLabel;
        private System.Windows.Forms.Label bRegLabel;
        private System.Windows.Forms.NumericUpDown bRegView;
        private System.Windows.Forms.Label cRegLabel;
        private System.Windows.Forms.NumericUpDown cRegView;
        private System.Windows.Forms.Label dRegLabel;
        private System.Windows.Forms.NumericUpDown dRegView;
        private System.Windows.Forms.Label eRegLabel;
        private System.Windows.Forms.NumericUpDown eRegView;
        private System.Windows.Forms.Label hRegLabel;
        private System.Windows.Forms.NumericUpDown hRegView;
        private System.Windows.Forms.Label lRegLabel;
        private System.Windows.Forms.NumericUpDown lRegView;
        private System.Windows.Forms.Label pcRegLabel;
        private System.Windows.Forms.NumericUpDown pcRegView;
        private System.Windows.Forms.Label spRegLabel;
        private System.Windows.Forms.NumericUpDown spRegView;
        private System.Windows.Forms.CheckBox runningChkBx;
        private System.Windows.Forms.Button stepBtn;
    }
}
