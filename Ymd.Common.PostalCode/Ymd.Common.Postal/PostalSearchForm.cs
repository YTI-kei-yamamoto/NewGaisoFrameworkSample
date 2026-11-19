using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ymd.Common.Postal
{
    internal partial class PostalSearchForm : Form
    {
        private readonly PostalDataAccess _dataAccess;
        private readonly AddressToZipTabControl _addressToZipTab;

        private PostalAddress _lastTab2Address;
        private string _lockedBaseAddress = string.Empty;
        private string _userSuffix = string.Empty;
        private bool _isUpdatingOutput;

        public string SelectedOutputText { get; private set; }
        public PostalAddress SelectedAddressData { get; private set; }

        public PostalSearchForm(string mdbPath)
        {
            InitializeComponent();
            _dataAccess = new PostalDataAccess(mdbPath);
            
            // Tab 1 setup (〒⇒住所)
            txtOutputGlobal.ReadOnly = true;
            txtOutputGlobal.KeyDown += TxtOutputGlobal_KeyDown;
            txtOutputGlobal.MouseUp += TxtOutputGlobal_MouseUp;
            txtOutputGlobal.TextChanged += TxtOutputGlobal_TextChanged;
            dgvCandidates.AutoGenerateColumns = false;
            dgvCandidates.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            
            // Tab 2 setup (住所⇒〒)
            _addressToZipTab = new AddressToZipTabControl(_dataAccess);
            _addressToZipTab.OutputTextChanged += AddressToZipTab_OutputTextChanged;
            _addressToZipTab.AddressConfirmed += (s, e) => btnOk.PerformClick();
            tabAddressToZip.Controls.Add(_addressToZipTab);

            tabControlMain.SelectedIndexChanged += TabControlMain_SelectedIndexChanged;
            chkAddPrefectureTab1.CheckedChanged += ChkAddPrefectureTab1_CheckedChanged;
        }

        private void txtZipCode_TextChanged(object sender, EventArgs e)
        {
            string zip = txtZipCode.Text.Trim();
            if (zip.Length >= 3)
            {
                var candidates = _dataAccess.SearchByPostalCode(zip, chkIgnoreBranch.Checked);
                dgvCandidates.DataSource = candidates;
            }
            else
            {
                dgvCandidates.DataSource = null;
                SetBaseAddress(string.Empty, string.Empty, keepUserSuffix: false, isEditable: false);
            }
        }

        private void dgvCandidates_SelectionChanged(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab != tabZipToAddress) return;

            if (dgvCandidates.CurrentRow?.DataBoundItem is PostalAddress address)
            {
                var parts = address.GetAddressParts(chkAddPrefectureTab1.Checked);
                SetBaseAddress(parts.LockedBase, parts.EditableSuffix, keepUserSuffix: false, isEditable: true);
            }
            else
            {
                SetBaseAddress(string.Empty, string.Empty, keepUserSuffix: false, isEditable: false);
            }
        }

        private void ChkAddPrefectureTab1_CheckedChanged(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab != tabZipToAddress) return;

            if (dgvCandidates.CurrentRow?.DataBoundItem is PostalAddress address)
            {
                var parts = address.GetAddressParts(chkAddPrefectureTab1.Checked);
                SetBaseAddress(parts.LockedBase, parts.EditableSuffix, keepUserSuffix: true, isEditable: true);
            }
        }

        private void AddressToZipTab_OutputTextChanged(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab != tabAddressToZip) return;
            SyncFromAddressToZipTab();
        }

        private void TabControlMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update global output box based on the active tab
            if (tabControlMain.SelectedTab == tabZipToAddress)
            {
                dgvCandidates_SelectionChanged(this, EventArgs.Empty);
            }
            else if (tabControlMain.SelectedTab == tabAddressToZip)
            {
                _lastTab2Address = null;
                SyncFromAddressToZipTab();
            }
        }

        private void SyncFromAddressToZipTab()
        {
            var currentAddr = _addressToZipTab.CurrentSelectedAddress;
            if (currentAddr != null)
            {
                var parts = currentAddr.GetAddressParts(_addressToZipTab.IsPrefectureChecked);
                bool isSameAddress = ReferenceEquals(_lastTab2Address, currentAddr) ||
                                     (_lastTab2Address != null &&
                                      _lastTab2Address.PostalCode == currentAddr.PostalCode &&
                                      _lastTab2Address.Town == currentAddr.Town);
                _lastTab2Address = currentAddr;
                SetBaseAddress(parts.LockedBase, parts.EditableSuffix, keepUserSuffix: isSameAddress, isEditable: true);
            }
            else
            {
                _lastTab2Address = null;
                SetBaseAddress(string.Empty, string.Empty, keepUserSuffix: false, isEditable: false);
            }
        }

        private void SetBaseAddress(string newBaseAddress, string initialSuffix, bool keepUserSuffix, bool isEditable)
        {
            _isUpdatingOutput = true;
            try
            {
                _lockedBaseAddress = newBaseAddress ?? string.Empty;
                if (!keepUserSuffix || !isEditable)
                {
                    _userSuffix = initialSuffix ?? string.Empty;
                }

                txtOutputGlobal.ReadOnly = !isEditable;
                txtOutputGlobal.Text = _lockedBaseAddress + _userSuffix;
                txtOutputGlobal.SelectionStart = txtOutputGlobal.Text.Length;
                txtOutputGlobal.SelectionLength = 0;
            }
            finally
            {
                _isUpdatingOutput = false;
            }
        }

        private void TxtOutputGlobal_KeyDown(object sender, KeyEventArgs e)
        {
            if (txtOutputGlobal.ReadOnly) return;

            int minPos = _lockedBaseAddress.Length;
            int selStart = txtOutputGlobal.SelectionStart;
            int selLength = txtOutputGlobal.SelectionLength;

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right ||
                e.KeyCode == Keys.Home || e.KeyCode == Keys.End ||
                (e.Control && e.KeyCode == Keys.C))
            {
                if (e.KeyCode == Keys.Home && !e.Shift)
                {
                    txtOutputGlobal.SelectionStart = minPos;
                    txtOutputGlobal.SelectionLength = 0;
                    e.SuppressKeyPress = true;
                }
                return;
            }

            if (selStart < minPos)
            {
                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete || (e.Control && e.KeyCode == Keys.X))
                {
                    e.SuppressKeyPress = true;
                    return;
                }
                txtOutputGlobal.SelectionStart = txtOutputGlobal.Text.Length;
                txtOutputGlobal.SelectionLength = 0;
            }
            else if (selStart == minPos && selLength == 0 && e.KeyCode == Keys.Back)
            {
                e.SuppressKeyPress = true;
            }
        }

        private void TxtOutputGlobal_MouseUp(object sender, MouseEventArgs e)
        {
            if (txtOutputGlobal.ReadOnly) return;
            if (txtOutputGlobal.SelectionLength == 0 && txtOutputGlobal.SelectionStart < _lockedBaseAddress.Length)
            {
                txtOutputGlobal.SelectionStart = _lockedBaseAddress.Length;
            }
        }

        private void TxtOutputGlobal_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingOutput || txtOutputGlobal.ReadOnly) return;

            string currentText = txtOutputGlobal.Text;
            if (!currentText.StartsWith(_lockedBaseAddress, StringComparison.Ordinal))
            {
                _isUpdatingOutput = true;
                try
                {
                    txtOutputGlobal.Text = _lockedBaseAddress + _userSuffix;
                    txtOutputGlobal.SelectionStart = txtOutputGlobal.Text.Length;
                    txtOutputGlobal.SelectionLength = 0;
                }
                finally
                {
                    _isUpdatingOutput = false;
                }
                return;

            }
            _userSuffix = currentText.Substring(_lockedBaseAddress.Length);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            PostalAddress rawAddress = null;

            if (tabControlMain.SelectedTab == tabZipToAddress)
            {
                if (dgvCandidates.CurrentRow?.DataBoundItem is PostalAddress address)
                {
                    rawAddress = address;
                }
            }
            else if (tabControlMain.SelectedTab == tabAddressToZip)
            {
                if (_addressToZipTab.CurrentSelectedAddress != null)
                {
                    rawAddress = _addressToZipTab.CurrentSelectedAddress;
                }
            }

            if (rawAddress != null)
            {
                // ユーザーが追記・編集したサフィックス(_userSuffix)を結合した新しいインスタンスを生成
                SelectedAddressData = rawAddress.WithEditedSuffix(_userSuffix);
                    SelectedOutputText = txtOutputGlobal.Text;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}