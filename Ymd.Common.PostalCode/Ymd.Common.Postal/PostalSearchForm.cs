using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ymd.Common.Postal
{
    internal partial class PostalSearchForm : Form
    {
        private readonly PostalDataAccess _dataAccess;
        private AddressToZipTabControl _addressToZipTab;

        public string SelectedOutputText { get; private set; }
        public PostalAddress SelectedAddressData { get; private set; }

        public PostalSearchForm(string mdbPath)
        {
            InitializeComponent();
            _dataAccess = new PostalDataAccess(mdbPath);
            
            // Tab 1 setup (§ËZŠ)
            dgvCandidates.AutoGenerateColumns = false;
            dgvCandidates.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            
            // Tab 2 setup (ZŠË§)
            _addressToZipTab = new AddressToZipTabControl(_dataAccess);
            _addressToZipTab.OutputTextChanged += AddressToZipTab_OutputTextChanged;
            _addressToZipTab.AddressConfirmed += (s, e) => btnOk.PerformClick();
            tabAddressToZip.Controls.Add(_addressToZipTab);

            tabControlMain.SelectedIndexChanged += TabControlMain_SelectedIndexChanged;
        }

        private void txtZipCode_TextChanged(object sender, EventArgs e)
        {
            string zip = txtZipCode.Text.Trim();
            if (zip.Length >= 3)
            {
                var candidates = _dataAccess.SearchByPostalCode(zip, chkIgnoreBranch.Checked);
                dgvCandidates.DataSource = candidates;
            }
        }

        private void dgvCandidates_SelectionChanged(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab == tabZipToAddress && dgvCandidates.CurrentRow?.DataBoundItem is PostalAddress address)
            {
                txtOutputGlobal.Text = address.GetOutputAddress(chkAddPrefectureTab1.Checked);
            }
        }

        private void AddressToZipTab_OutputTextChanged(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab == tabAddressToZip)
            {
                txtOutputGlobal.Text = _addressToZipTab.OutputAddressText;
            }
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
                txtOutputGlobal.Text = _addressToZipTab.OutputAddressText;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab == tabZipToAddress)
            {
                if (dgvCandidates.CurrentRow?.DataBoundItem is PostalAddress address)
                {
                    SelectedAddressData = address;
                    SelectedOutputText = txtOutputGlobal.Text;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            else if (tabControlMain.SelectedTab == tabAddressToZip)
            {
                if (_addressToZipTab.CurrentSelectedAddress != null)
                {
                    SelectedAddressData = _addressToZipTab.CurrentSelectedAddress;
                    SelectedOutputText = txtOutputGlobal.Text;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}