using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Ymd.Gaiso.Simulator.Models;
using Ymd.Gaiso.Simulator.Services;

namespace Ymd.Gaiso.Simulator.Controls
{
    public class MaterialCatalogDialog : Form
    {
        private readonly IMaterialCatalogRepository _catalogRepository;
        public MaterialCatalogItem SelectedCatalogItem { get; private set; }

        private DataGridView gridCatalog;
        private ComboBox cmbCategoryFilter;
        private TextBox txtKeyword;
        private Button btnSelect;
        private Button btnCancel;
        private Label lblFilter;
        private Label lblSearch;

        public MaterialCatalogDialog(IMaterialCatalogRepository catalogRepository)
        {
            _catalogRepository = catalogRepository ?? throw new ArgumentNullException(nameof(catalogRepository));
            InitializeComponents();
            ApplyLocalization();
            LoadCatalogData();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(840, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Yu Gothic UI", 9F);

            var panelTop = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(10) };
            lblFilter = new Label { Location = new Point(12, 13), AutoSize = true };
            cmbCategoryFilter = new ComboBox { Location = new Point(95, 10), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategoryFilter.Items.Add("すべて");
            foreach (var cat in Enum.GetNames(typeof(MaterialCategory))) cmbCategoryFilter.Items.Add(cat);
            cmbCategoryFilter.SelectedIndex = 0;
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => LoadCatalogData();

            lblSearch = new Label { Location = new Point(255, 13), AutoSize = true };
            txtKeyword = new TextBox { Location = new Point(350, 10), Width = 200 };
            txtKeyword.TextChanged += (s, e) => LoadCatalogData();

            panelTop.Controls.AddRange(new Control[] { lblFilter, cmbCategoryFilter, lblSearch, txtKeyword });

            gridCatalog = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White
            };

            gridCatalog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "型番コード", DataPropertyName = "CatalogCode", Width = 120 });
            gridCatalog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "部材名称", DataPropertyName = "Name", Width = 170 });
            gridCatalog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "区分", DataPropertyName = "Category", Width = 80 });
            gridCatalog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "材質 / 建具種別", DataPropertyName = "MaterialOrType", Width = 130 });
            gridCatalog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "板厚 / 寸法(mm)", DataPropertyName = "SizeOrThickness", Width = 110 });
            gridCatalog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "カラー / 仕様", DataPropertyName = "ColorOrOptions", Width = 190 });
            gridCatalog.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ConfirmSelection(); };

            var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 46 };
            btnSelect = new Button { Location = new Point(600, 8), Size = new Size(115, 30), BackColor = Color.FromArgb(18, 118, 224), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel = new Button { Location = new Point(722, 8), Size = new Size(90, 30), DialogResult = DialogResult.Cancel };
            btnSelect.Click += (s, e) => ConfirmSelection();

            panelBottom.Controls.AddRange(new Control[] { btnSelect, btnCancel });
            this.Controls.AddRange(new Control[] { gridCatalog, panelTop, panelBottom });
            this.AcceptButton = btnSelect;
            this.CancelButton = btnCancel;
        }

        private void ApplyLocalization()
        {
            this.Text = LocalizationService.T("Dlg.CatalogTitle");
            lblFilter.Text = LocalizationService.T("Dlg.Category");
            lblSearch.Text = LocalizationService.T("Dlg.Keyword");
            btnSelect.Text = LocalizationService.T("Dlg.SelectAdd");
            btnCancel.Text = LocalizationService.T("Dlg.Cancel");
        }

        private void ConfirmSelection()
        {
            if (gridCatalog.CurrentRow != null)
            {
                dynamic data = gridCatalog.CurrentRow.DataBoundItem;
                SelectedCatalogItem = data.RawItem;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("外装材または建具を選択してください。", "選択確認", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoadCatalogData()
        {
            List<MaterialCatalogItem> items;
            if (cmbCategoryFilter.SelectedIndex <= 0) items = _catalogRepository.GetAllCatalogItems();
            else items = _catalogRepository.GetCatalogItemsByCategory((MaterialCategory)Enum.Parse(typeof(MaterialCategory), cmbCategoryFilter.SelectedItem.ToString()));

            string kw = txtKeyword?.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(kw))
            {
                items = items.Where(i => (i.Name != null && i.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) || (i.CatalogCode != null && i.CatalogCode.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            }

            var displayList = items.Select(item => new
            {
                RawItem = item,
                item.CatalogCode,
                item.Name,
                Category = item.Category.ToString(),
                MaterialOrType = item.Category == MaterialCategory.Fittings ? item.FittingsType : item.Material,
                SizeOrThickness = item.Category == MaterialCategory.Fittings ? $"W{item.DefaultWidth:F0}×H{item.DefaultHeight:F0}" : $"{item.Thickness:F1} mm",
                ColorOrOptions = item.Category == MaterialCategory.Fittings ? item.Options : item.Color
            }).ToList();

            gridCatalog.DataSource = displayList;
        }
    }
}
