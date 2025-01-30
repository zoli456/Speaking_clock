namespace Speaking_Clock
{
    partial class CustomWarningForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomWarningForm));
            label1 = new Label();
            CustomWarning_Amount = new TextBox();
            button1 = new Button();
            CustomWarning_Cancel = new Button();
            label3 = new Label();
            label2 = new Label();
            Custom_Hour = new TextBox();
            Custom_Minutes = new TextBox();
            label4 = new Label();
            label5 = new Label();
            Repeate_checkbox = new CheckBox();
            label6 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(93, 20);
            label1.Name = "label1";
            label1.Size = new Size(146, 21);
            label1.TabIndex = 0;
            label1.Text = "Ennyi perc múlva:";
            // 
            // CustomWarning_Amount
            // 
            CustomWarning_Amount.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            CustomWarning_Amount.Location = new Point(245, 14);
            CustomWarning_Amount.MaxLength = 3;
            CustomWarning_Amount.Name = "CustomWarning_Amount";
            CustomWarning_Amount.Size = new Size(100, 27);
            CustomWarning_Amount.TabIndex = 1;
            CustomWarning_Amount.TextAlign = HorizontalAlignment.Center;
            CustomWarning_Amount.Enter += CustomWarning_Amount_Enter;
            CustomWarning_Amount.KeyPress += CustomWarning_Amount_KeyPress;
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
            button1.Location = new Point(364, 146);
            button1.Name = "button1";
            button1.Size = new Size(81, 32);
            button1.TabIndex = 2;
            button1.Text = "Rendben";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // CustomWarning_Cancel
            // 
            CustomWarning_Cancel.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
            CustomWarning_Cancel.Location = new Point(12, 146);
            CustomWarning_Cancel.Name = "CustomWarning_Cancel";
            CustomWarning_Cancel.Size = new Size(81, 32);
            CustomWarning_Cancel.TabIndex = 3;
            CustomWarning_Cancel.Text = "Mégsem";
            CustomWarning_Cancel.UseVisualStyleBackColor = true;
            CustomWarning_Cancel.Click += CustomWarning_Cancel_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(210, 60);
            label3.Name = "label3";
            label3.Size = new Size(32, 15);
            label3.TabIndex = 4;
            label3.Text = "vagy";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label2.Location = new Point(12, 104);
            label2.Name = "label2";
            label2.Size = new Size(125, 21);
            label2.TabIndex = 5;
            label2.Text = "Pontos időben:";
            // 
            // Custom_Hour
            // 
            Custom_Hour.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            Custom_Hour.Location = new Point(183, 98);
            Custom_Hour.MaxLength = 2;
            Custom_Hour.Name = "Custom_Hour";
            Custom_Hour.Size = new Size(100, 27);
            Custom_Hour.TabIndex = 6;
            Custom_Hour.TextAlign = HorizontalAlignment.Center;
            Custom_Hour.Enter += Custom_Hour_Enter;
            // 
            // Custom_Minutes
            // 
            Custom_Minutes.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            Custom_Minutes.Location = new Point(345, 98);
            Custom_Minutes.MaxLength = 2;
            Custom_Minutes.Name = "Custom_Minutes";
            Custom_Minutes.Size = new Size(100, 27);
            Custom_Minutes.TabIndex = 7;
            Custom_Minutes.TextAlign = HorizontalAlignment.Center;
            Custom_Minutes.Enter += Custom_Minutes_Enter;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(143, 105);
            label4.Name = "label4";
            label4.Size = new Size(34, 20);
            label4.TabIndex = 8;
            label4.Text = "Óra";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            label5.Location = new Point(300, 105);
            label5.Name = "label5";
            label5.Size = new Size(39, 20);
            label5.TabIndex = 9;
            label5.Text = "Perc";
            // 
            // Repeate_checkbox
            // 
            Repeate_checkbox.AutoSize = true;
            Repeate_checkbox.Enabled = false;
            Repeate_checkbox.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            Repeate_checkbox.Location = new Point(183, 153);
            Repeate_checkbox.Name = "Repeate_checkbox";
            Repeate_checkbox.Size = new Size(92, 25);
            Repeate_checkbox.TabIndex = 10;
            Repeate_checkbox.Text = "Ismétlés";
            Repeate_checkbox.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label6.Location = new Point(12, 104);
            label6.Name = "label6";
            label6.Size = new Size(125, 21);
            label6.TabIndex = 5;
            label6.Text = "Pontos időben:";
            // 
            // CustomWarning_Form
            // 
            AcceptButton = button1;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = CustomWarning_Cancel;
            ClientSize = new Size(457, 190);
            Controls.Add(Repeate_checkbox);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(Custom_Minutes);
            Controls.Add(Custom_Hour);
            Controls.Add(label6);
            Controls.Add(label2);
            Controls.Add(label3);
            Controls.Add(CustomWarning_Cancel);
            Controls.Add(button1);
            Controls.Add(CustomWarning_Amount);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CustomWarningForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Egyedi figyelmeztetés";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox CustomWarning_Amount;
        private Button button1;
        private Button CustomWarning_Cancel;
        private Label label3;
        private Label label2;
        private TextBox Custom_Hour;
        private TextBox Custom_Minutes;
        private Label label4;
        private Label label5;
        private CheckBox Repeate_checkbox;
        private Label label6;
    }
}