namespace en2ki
{
    partial class NotebookSelection
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
            this.cbNotebookList = new System.Windows.Forms.CheckedListBox();
            this.btnContinue = new System.Windows.Forms.Button();
            this.cbAll = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // cbNotebookList
            // 
            this.cbNotebookList.CheckOnClick = true;
            this.cbNotebookList.FormattingEnabled = true;
            this.cbNotebookList.Location = new System.Drawing.Point(13, 13);
            this.cbNotebookList.Name = "cbNotebookList";
            this.cbNotebookList.Size = new System.Drawing.Size(259, 214);
            this.cbNotebookList.TabIndex = 0;
            // 
            // btnContinue
            // 
            this.btnContinue.Location = new System.Drawing.Point(158, 233);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(114, 23);
            this.btnContinue.TabIndex = 1;
            this.btnContinue.Text = "Continue";
            this.btnContinue.UseVisualStyleBackColor = true;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            // 
            // cbAll
            // 
            this.cbAll.AutoSize = true;
            this.cbAll.Location = new System.Drawing.Point(13, 237);
            this.cbAll.Name = "cbAll";
            this.cbAll.Size = new System.Drawing.Size(120, 17);
            this.cbAll.TabIndex = 2;
            this.cbAll.Text = "Check/Uncheck All";
            this.cbAll.UseVisualStyleBackColor = true;
            this.cbAll.CheckedChanged += new System.EventHandler(this.cbAll_CheckedChanged);
            // 
            // NotebookSelection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.cbAll);
            this.Controls.Add(this.btnContinue);
            this.Controls.Add(this.cbNotebookList);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NotebookSelection";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Select notebooks to export";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox cbNotebookList;
        private System.Windows.Forms.Button btnContinue;
        private System.Windows.Forms.CheckBox cbAll;
    }
}