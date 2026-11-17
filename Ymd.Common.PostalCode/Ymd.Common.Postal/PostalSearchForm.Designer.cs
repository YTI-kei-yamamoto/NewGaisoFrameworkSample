namespace Ymd.Common.Postal
{
    partial class PostalSearchForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabZipToAddress = new System.Windows.Forms.TabPage();
            this.lblZip = new System.Windows.Forms.Label();
            this.txtZipCode = new System.Windows.Forms.TextBox();
            this.chkIgnoreBranch = new System.Windows.Forms.CheckBox();
            this.chkAddPrefectureTab1 = new System.Windows.Forms.CheckBox();
            this.lblCandidates = new System.Windows.Forms.Label();
            this.dgvCandidates = new System.Windows.Forms.DataGridView();
            this.colZip = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabAddressToZip = new System.Windows.Forms.TabPage();
            this.lblOutputGlobal = new System.Windows.Forms.Label();
            this.txtOutputGlobal = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            //this.btnHelp = new System.Windows.Forms.Button();
            this.tabControlMain.SuspendLayout();
            this.tabZipToAddress.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCandidates)).BeginInit();
            this.SuspendLayout();
            
            // tabControlMain
            this.tabControlMain.Controls.Add(this.tabZipToAddress);
            this.tabControlMain.Controls.Add(this.tabAddressToZip);
            this.tabControlMain.Location = new System.Drawing.Point(12, 12);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(500, 420);
            this.tabControlMain.Anchor = ((System.Windows.Forms.AnchorStyles)(
                ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right));
            
            // tabZipToAddress
            this.tabZipToAddress.Controls.Add(this.dgvCandidates);
            this.tabZipToAddress.Controls.Add(this.lblCandidates);
            this.tabZipToAddress.Controls.Add(this.chkAddPrefectureTab1);
            this.tabZipToAddress.Controls.Add(this.chkIgnoreBranch);
            this.tabZipToAddress.Controls.Add(this.txtZipCode);
            this.tabZipToAddress.Controls.Add(this.lblZip);
            this.tabZipToAddress.Location = new System.Drawing.Point(4, 24);
            this.tabZipToAddress.Name = "tabZipToAddress";
            this.tabZipToAddress.Size = new System.Drawing.Size(492, 392);
            this.tabZipToAddress.Text = "〒⇒住所";
            this.tabZipToAddress.UseVisualStyleBackColor = true;
            
            // Elements in tabZipToAddress
            this.lblZip.AutoSize = true; this.lblZip.Location = new System.Drawing.Point(12, 20); this.lblZip.Text = "〒(Z):";
            this.txtZipCode.Location = new System.Drawing.Point(60, 17); this.txtZipCode.Size = new System.Drawing.Size(120, 23); this.txtZipCode.TextChanged += new System.EventHandler(this.txtZipCode_TextChanged);
            
            this.chkIgnoreBranch.AutoSize = true;
            this.chkIgnoreBranch.Checked = true;
            this.chkIgnoreBranch.Location = new System.Drawing.Point(320, 19); 
            this.chkIgnoreBranch.Text = "枝番を考慮しない(&S)"; 
            this.chkIgnoreBranch.CheckedChanged += new System.EventHandler(this.txtZipCode_TextChanged);
            this.chkIgnoreBranch.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            
            this.chkAddPrefectureTab1.AutoSize = true; 
            this.chkAddPrefectureTab1.Checked = true; 
            this.chkAddPrefectureTab1.Location = new System.Drawing.Point(320, 42); 
            this.chkAddPrefectureTab1.Text = "都道府県名を付加(&T)"; 
            this.chkAddPrefectureTab1.CheckedChanged += new System.EventHandler(this.dgvCandidates_SelectionChanged);
            this.chkAddPrefectureTab1.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            
            this.lblCandidates.AutoSize = true; this.lblCandidates.Location = new System.Drawing.Point(12, 50); this.lblCandidates.Text = "住所候補(A):";
            
            this.dgvCandidates.Location = new System.Drawing.Point(12, 70); 
            this.dgvCandidates.Size = new System.Drawing.Size(468, 310); 
            this.dgvCandidates.RowHeadersVisible = false; 
            this.dgvCandidates.AllowUserToAddRows = false; 
            this.dgvCandidates.ReadOnly = true; 
            this.dgvCandidates.SelectionChanged += new System.EventHandler(this.dgvCandidates_SelectionChanged);
            this.dgvCandidates.Anchor = ((System.Windows.Forms.AnchorStyles)(
                ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right));
            
            this.colZip.HeaderText = "〒"; this.colZip.DataPropertyName = "FormattedPostalCode"; this.colZip.Width = 80;
            this.colAddress.HeaderText = "住所"; this.colAddress.DataPropertyName = "DisplayTown"; this.colAddress.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dgvCandidates.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.colZip, this.colAddress });
            
            // tabAddressToZip
            this.tabAddressToZip.Location = new System.Drawing.Point(4, 24);
            this.tabAddressToZip.Name = "tabAddressToZip";
            this.tabAddressToZip.Size = new System.Drawing.Size(492, 392);
            this.tabAddressToZip.Text = "住所⇒〒";
            this.tabAddressToZip.UseVisualStyleBackColor = true;

            // Global Output Field (Moved outside TabControl, above buttons)
            this.lblOutputGlobal.AutoSize = true; 
            this.lblOutputGlobal.Location = new System.Drawing.Point(20, 443); 
            this.lblOutputGlobal.Text = "出力(&O):";
            this.lblOutputGlobal.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            
            this.txtOutputGlobal.Location = new System.Drawing.Point(80, 440); 
            this.txtOutputGlobal.Size = new System.Drawing.Size(425, 23); 
            this.txtOutputGlobal.ReadOnly = false; // Made editable
            this.txtOutputGlobal.BackColor = System.Drawing.SystemColors.Window; // Set to standard editable background
            this.txtOutputGlobal.Anchor = ((System.Windows.Forms.AnchorStyles)(
                (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right));

            // Buttons
            this.btnOk.Location = new System.Drawing.Point(230, 480); 
            this.btnOk.Size = new System.Drawing.Size(80, 28); 
            this.btnOk.Text = "OK"; 
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right));
            
            this.btnCancel.Location = new System.Drawing.Point(320, 480); 
            this.btnCancel.Size = new System.Drawing.Size(80, 28); 
            this.btnCancel.Text = "キャンセル"; 
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right));
            
            //this.btnHelp.Location = new System.Drawing.Point(410, 480); 
            //this.btnHelp.Size = new System.Drawing.Size(80, 28); 
            //this.btnHelp.Text = "ヘルプ(&H)";
            //this.btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right));

            // Form
            this.ClientSize = new System.Drawing.Size(524, 521);
            this.MinimumSize = new System.Drawing.Size(540, 560);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.Controls.Add(this.lblOutputGlobal);
            this.Controls.Add(this.txtOutputGlobal);
            //this.Controls.Add(this.btnHelp);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.tabControlMain);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "住所の入力";
            
            this.tabControlMain.ResumeLayout(false);
            this.tabZipToAddress.ResumeLayout(false);
            this.tabZipToAddress.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCandidates)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabZipToAddress;
        private System.Windows.Forms.TabPage tabAddressToZip;
        private System.Windows.Forms.Label lblZip;
        private System.Windows.Forms.TextBox txtZipCode;
        private System.Windows.Forms.CheckBox chkIgnoreBranch;
        private System.Windows.Forms.CheckBox chkAddPrefectureTab1;
        private System.Windows.Forms.Label lblCandidates;
        private System.Windows.Forms.DataGridView dgvCandidates;
        private System.Windows.Forms.DataGridViewTextBoxColumn colZip;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAddress;
        private System.Windows.Forms.Label lblOutputGlobal;
        private System.Windows.Forms.TextBox txtOutputGlobal;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        //private System.Windows.Forms.Button btnHelp;
    }
}