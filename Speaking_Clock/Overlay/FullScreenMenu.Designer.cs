namespace Speaking_Clock
{
    partial class FullScreenMenu
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
            components = new System.ComponentModel.Container();
            radListControl1 = new Telerik.WinControls.UI.RadListControl();
            radLabel1 = new Telerik.WinControls.UI.RadLabel();
            radButton1 = new Telerik.WinControls.UI.RadButton();
            radLabel2 = new Telerik.WinControls.UI.RadLabel();
            timeUpdater = new System.Windows.Forms.Timer(components);
            AutoClosetimer = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)radListControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)radLabel1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)radButton1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)radLabel2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this).BeginInit();
            SuspendLayout();
            // 
            // radListControl1
            // 
            radListControl1.AutoSizeItems = true;
            radListControl1.Location = new Point(12, 70);
            radListControl1.Name = "radListControl1";
            radListControl1.Size = new Size(152, 159);
            radListControl1.TabIndex = 0;
            // 
            // radLabel1
            // 
            radLabel1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            radLabel1.Location = new Point(34, 12);
            radLabel1.Name = "radLabel1";
            radLabel1.Size = new Size(116, 21);
            radLabel1.TabIndex = 1;
            radLabel1.Text = "Értesítés beállítás";
            radLabel1.TextAlignment = ContentAlignment.TopLeft;
            // 
            // radButton1
            // 
            radButton1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            radButton1.Location = new Point(9, 235);
            radButton1.Name = "radButton1";
            radButton1.Size = new Size(155, 24);
            radButton1.TabIndex = 2;
            radButton1.Text = "Rendben";
            radButton1.Click += radButton1_Click;
            // 
            // radLabel2
            // 
            radLabel2.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            radLabel2.Location = new Point(22, 43);
            radLabel2.Name = "radLabel2";
            radLabel2.Size = new Size(128, 21);
            radLabel2.TabIndex = 3;
            radLabel2.Text = "Jelenlegi idő: 00:00";
            // 
            // timeUpdater
            // 
            timeUpdater.Enabled = true;
            timeUpdater.Interval = 1000;
            timeUpdater.Tick += timeUpdater_Tick;
            // 
            // AutoClosetimer
            // 
            AutoClosetimer.Enabled = true;
            AutoClosetimer.Interval = 20000;
            AutoClosetimer.Tick += AutoClosetimer_Tick;
            // 
            // FullScreenMenu
            // 
            AutoScaleBaseSize = new Size(7, 15);
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(176, 267);
            Controls.Add(radLabel2);
            Controls.Add(radButton1);
            Controls.Add(radLabel1);
            Controls.Add(radListControl1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "FullScreenMenu";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Értesítés";
            TopMost = true;
            FormClosing += FullScreenMenu_FormClosing;
            FormClosed += FullScreenMenu_FormClosed;
            Shown += FullScreenMenu_Shown;
            ((System.ComponentModel.ISupportInitialize)radListControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)radLabel1).EndInit();
            ((System.ComponentModel.ISupportInitialize)radButton1).EndInit();
            ((System.ComponentModel.ISupportInitialize)radLabel2).EndInit();
            ((System.ComponentModel.ISupportInitialize)this).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Telerik.WinControls.UI.RadListControl radListControl1;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadButton radButton1;
        internal Telerik.WinControls.UI.RadLabel radLabel2;
        private System.Windows.Forms.Timer timeUpdater;
        private System.Windows.Forms.Timer AutoClosetimer;
    }
}
