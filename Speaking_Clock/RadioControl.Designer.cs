namespace Speaking_Clock
{
    partial class RadioControl
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
            Radiolabel = new Label();
            RadioVolumetrackBar = new TrackBar();
            button1 = new Button();
            RadiotrackBar = new TrackBar();
            checkBox1 = new CheckBox();
            Volumelabel = new Label();
            ((System.ComponentModel.ISupportInitialize)RadioVolumetrackBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)RadiotrackBar).BeginInit();
            SuspendLayout();
            // 
            // Radiolabel
            // 
            Radiolabel.AutoSize = true;
            Radiolabel.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 238);
            Radiolabel.Location = new Point(12, 9);
            Radiolabel.Name = "Radiolabel";
            Radiolabel.Size = new Size(100, 20);
            Radiolabel.TabIndex = 0;
            Radiolabel.Text = "Valami Rádió";
            // 
            // RadioVolumetrackBar
            // 
            RadioVolumetrackBar.Location = new Point(155, 2);
            RadioVolumetrackBar.Maximum = 100;
            RadioVolumetrackBar.Minimum = 1;
            RadioVolumetrackBar.Name = "RadioVolumetrackBar";
            RadioVolumetrackBar.Size = new Size(224, 45);
            RadioVolumetrackBar.TabIndex = 1;
            RadioVolumetrackBar.Value = 1;
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            button1.Location = new Point(412, 12);
            button1.Name = "button1";
            button1.Size = new Size(78, 25);
            button1.TabIndex = 2;
            button1.Text = "Megállít";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // RadiotrackBar
            // 
            RadiotrackBar.Location = new Point(143, 2);
            RadiotrackBar.Maximum = 100;
            RadiotrackBar.Minimum = 1;
            RadiotrackBar.Name = "RadiotrackBar";
            RadiotrackBar.Size = new Size(224, 45);
            RadiotrackBar.TabIndex = 1;
            RadiotrackBar.Value = 1;
            RadiotrackBar.Scroll += RadiotrackBar_Scroll;
            RadiotrackBar.MouseDown += RadiotrackBar_MouseDown;
            RadiotrackBar.MouseUp += RadiotrackBar_MouseUp;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(33, 28);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(58, 19);
            checkBox1.TabIndex = 3;
            checkBox1.Text = "Húzás";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // Volumelabel
            // 
            Volumelabel.AutoSize = true;
            Volumelabel.Location = new Point(247, 29);
            Volumelabel.Name = "Volumelabel";
            Volumelabel.Size = new Size(23, 15);
            Volumelabel.TabIndex = 4;
            Volumelabel.Text = "0%";
            // 
            // RadioControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(502, 48);
            ControlBox = false;
            Controls.Add(Volumelabel);
            Controls.Add(checkBox1);
            Controls.Add(button1);
            Controls.Add(RadiotrackBar);
            Controls.Add(RadioVolumetrackBar);
            Controls.Add(Radiolabel);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 238);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RadioControl";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "RadioControl";
            Shown += RadioControl_Shown;
            MouseDown += RadioControl_MouseDown;
            ((System.ComponentModel.ISupportInitialize)RadioVolumetrackBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)RadiotrackBar).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        internal Label Radiolabel;
        private TrackBar RadioVolumetrackBar;
        private Button button1;
        private CheckBox checkBox1;
        private Label Volumelabel;
        internal static TrackBar RadiotrackBar;
    }
}