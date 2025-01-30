namespace Speaking_Clock
{
    partial class SystemInformation
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SystemInformation));
            InfotextBox = new TextBox();
            SuspendLayout();
            // 
            // InfotextBox
            // 
            InfotextBox.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 238);
            InfotextBox.Location = new Point(6, 14);
            InfotextBox.Multiline = true;
            InfotextBox.Name = "InfotextBox";
            InfotextBox.ScrollBars = ScrollBars.Vertical;
            InfotextBox.Size = new Size(438, 397);
            InfotextBox.TabIndex = 0;
            // 
            // SystemInformation
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(456, 425);
            Controls.Add(InfotextBox);
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 238);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SystemInformation";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Információ";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        internal TextBox InfotextBox;
    }
}