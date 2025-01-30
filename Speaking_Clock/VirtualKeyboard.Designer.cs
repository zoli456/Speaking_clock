namespace Speaking_Clock
{
    partial class VirtualKeyboard
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
            radCheckBox1 = new Telerik.WinControls.UI.RadCheckBox();
            radVirtualKeyboard1 = new Telerik.WinControls.UI.RadVirtualKeyboard();
            ((System.ComponentModel.ISupportInitialize)radCheckBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this).BeginInit();
            ((System.ComponentModel.ISupportInitialize)radVirtualKeyboard1).BeginInit();
            radVirtualKeyboard1.SuspendLayout();
            SuspendLayout();
            // 
            // radCheckBox1
            // 
            radCheckBox1.Location = new Point(610, 34);
            radCheckBox1.Name = "radCheckBox1";
            radCheckBox1.Size = new Size(79, 18);
            radCheckBox1.TabIndex = 0;
            radCheckBox1.Text = "Szürke mód";
            radCheckBox1.ToggleStateChanged += radCheckBox1_ToggleStateChanged;
            // 
            // VirtualKeyboard
            // 
            AutoScaleBaseSize = new Size(7, 15);
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 337);
            // 
            // radVirtualKeyboard1
            // 
            radVirtualKeyboard1.Controls.Add(radCheckBox1);
            radVirtualKeyboard1.EnableAnalytics = false;
            radVirtualKeyboard1.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 238);
            radVirtualKeyboard1.Location = new Point(0, 2);
            radVirtualKeyboard1.Name = "radVirtualKeyboard1";
            radVirtualKeyboard1.Size = new Size(801, 337);
            radVirtualKeyboard1.TabIndex = 0;
            radVirtualKeyboard1.TabStop = false;
            ((Telerik.WinControls.UI.RadVirtualKeyboardElement)radVirtualKeyboard1.GetChildAt(0)).Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            Controls.Add(radVirtualKeyboard1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "VirtualKeyboard";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Virtuális Billentyűzet";
            TopMost = true;
            ((System.ComponentModel.ISupportInitialize)radCheckBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)radVirtualKeyboard1).EndInit();
            radVirtualKeyboard1.ResumeLayout(false);
            radVirtualKeyboard1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)this).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Telerik.WinControls.UI.RadVirtualKeyboard radVirtualKeyboard1;
        internal Telerik.WinControls.UI.RadCheckBox radCheckBox1;
    }
}
