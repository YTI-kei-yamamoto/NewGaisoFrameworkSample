using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ymd.Gaiso.Simulator.Infrastructure;
using Ymd.Gaiso.Simulator.Models;
using Ymd.Gaiso.Simulator.Controls;
using Ymd.Gaiso.Simulator.Services;

namespace Ymd.Gaiso.Simulator
{
    public partial class MainForm : Form
    {
        private KojiData _kojiData;
        private string _exchangeJsonPath;
        private GaisoSurface _selectedSurface;
        private readonly IMaterialCatalogRepository _catalogRepository;
        private UserLayoutSettings _layoutSettings;
        private Rectangle _lastNormalBounds;

        public MainForm(KojiData kojiData, string exchangeJsonPath)
        {
            InitializeComponent();

            _kojiData = kojiData ?? KojiData.CreateDefaultSample();
            _exchangeJsonPath = exchangeJsonPath;
            
            string catalogDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MaterialCatalog.db");
            _catalogRepository = new SqliteMaterialCatalogRepository(catalogDbPath);

            _layoutSettings = UserLayoutSettings.Load();
            _lastNormalBounds = this.Bounds;
            
            LocalizationService.CurrentLanguage = _layoutSettings.Language ?? LocalizationService.LangJa;
            cmbLanguage.SelectedIndex = LocalizationService.CurrentLanguage switch { LocalizationService.LangEn => 1, LocalizationService.LangZh => 2, _ => 0 };
            ApplyLocalization();

            this.Move += (s, e) => { if (this.WindowState == FormWindowState.Normal) _lastNormalBounds = this.Bounds; };
            this.Resize += (s, e) => { if (this.WindowState == FormWindowState.Normal) _lastNormalBounds = this.Bounds; };
            this.Shown += (s, e) => { ApplySavedLayoutSettings(_layoutSettings); _viewport3D.SwitchTo3DMode(); };
            this.FormClosing += (s, e) => SaveCurrentLayoutSettings();

            BuildModelTreeNodes();
            _selectedSurface = _kojiData.GaisoModel.AllSurfaces.FirstOrDefault();
            LoadSurfaceToPropertyPanel(_selectedSurface);
            _viewport3D.BindKojiData(_kojiData, _selectedSurface?.SurfaceId);
            _viewport3D.SwitchTo3DMode();
        }

        private void CmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            LocalizationService.CurrentLanguage = cmbLanguage.SelectedIndex switch { 1 => LocalizationService.LangEn, 2 => LocalizationService.LangZh, _ => LocalizationService.LangJa };
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            this.SuspendLayout();
            this.Text = $"{LocalizationService.T("App.Title")} - {_kojiData.KojiName} ({_kojiData.KojiNo})";

            foreach (var kvp in _ribbonTabButtons) kvp.Value.Text = LocalizationService.T(kvp.Key);
            foreach (var item in _ribbonGroupLabels) item.LabelCtrl.Text = LocalizationService.T(item.Key);
            foreach (var item in _ribbonCommandButtons) { item.BtnCtrl.Text = LocalizationService.T(item.Key); item.BtnCtrl.Font = new Font("Yu Gothic UI", 7.6f, (item.Key == "Cmd.Select" || item.Key == "Cmd.Realistic") ? FontStyle.Bold : FontStyle.Regular); }

            if (lblModelTreeHeader != null) lblModelTreeHeader.Text = LocalizationService.T("Header.ModelTree");
            if (lblRightPaneHeader != null) lblRightPaneHeader.Text = LocalizationService.T("Header.RightPane");
            if (lblComponentsTitle != null) lblComponentsTitle.Text = LocalizationService.T("Label.CompList");

            btnAddPanel.Text = LocalizationService.T("Cmd.AddPanel"); btnAddMat.Text = LocalizationService.T("Cmd.AddMat"); btnAddBoard.Text = LocalizationService.T("Cmd.AddBoard"); btnAddFittings.Text = LocalizationService.T("Cmd.AddFittings");
            btnAddFromCatalog.Text = LocalizationService.T("Cmd.AddCatalog"); btnRemoveComponent.Text = LocalizationService.T("Cmd.Remove"); btnMoveUpComponent.Text = LocalizationService.T("Cmd.MoveUp"); btnMoveDownComponent.Text = LocalizationService.T("Cmd.MoveDown");

            var currentObj = propertyGridComponent.SelectedObject;
            propertyGridComponent.SelectedObject = null; propertyGridComponent.SelectedObject = currentObj;
            UpdatePropertyGrid(currentObj as ExteriorMaterialComponent);

            _viewport3D?.RefreshLocalizedTexts();
            lblStatusText.Text = LocalizationService.T("Status.Ready");
            this.ResumeLayout();
        }

        private void SelectRibbonTab(string tabKey)
        {
            foreach (var kvp in _ribbonTabButtons)
            {
                bool isSelected = (kvp.Key == tabKey);
                if (kvp.Key == "Tab.File") { kvp.Value.BackColor = isSelected ? Color.FromArgb(16, 92, 184) : Color.FromArgb(24, 114, 214); kvp.Value.ForeColor = Color.White; kvp.Value.Font = new Font("Yu Gothic UI", 9f, FontStyle.Bold); }
                else { kvp.Value.BackColor = isSelected ? Color.White : Color.Transparent; kvp.Value.ForeColor = isSelected ? Color.FromArgb(24, 114, 214) : Color.FromArgb(55, 65, 80); kvp.Value.Font = new Font("Yu Gothic UI", 9f, isSelected ? FontStyle.Bold : FontStyle.Regular); }
            }
            foreach (var kvp in _ribbonTabPanels) kvp.Value.Visible = (kvp.Key == tabKey);
        }

        private void SetPaneRatios(double leftRatio, double centerRatio, double rightRatio)
        {
            double total = leftRatio + centerRatio + rightRatio; if (total <= 0) return;
            int totalWidth = splitLeftRight.ClientSize.Width; int maxLeft = totalWidth - splitLeftRight.Panel2MinSize - splitLeftRight.SplitterWidth;
            if (totalWidth > 0 && maxLeft >= splitLeftRight.Panel1MinSize) splitLeftRight.SplitterDistance = Math.Max(splitLeftRight.Panel1MinSize, Math.Min(maxLeft, (int)(totalWidth * (leftRatio / total))));
            int remainingWidth = splitCenterHorizontal.ClientSize.Width; int maxCenter = remainingWidth - splitCenterHorizontal.Panel2MinSize - splitCenterHorizontal.SplitterWidth;
            if (remainingWidth > 0 && maxCenter >= splitCenterHorizontal.Panel1MinSize) splitCenterHorizontal.SplitterDistance = Math.Max(splitCenterHorizontal.Panel1MinSize, Math.Min(maxCenter, (int)(remainingWidth * (centerRatio / (centerRatio + rightRatio)))));
        }

        private void ApplySavedLayoutSettings(UserLayoutSettings settings)
        {
            if (settings == null) return;
            if (settings.WindowWidth >= this.MinimumSize.Width && settings.WindowHeight >= this.MinimumSize.Height) { var targetRect = new Rectangle(settings.WindowX >= 0 ? settings.WindowX : this.Left, settings.WindowY >= 0 ? settings.WindowY : this.Top, settings.WindowWidth, settings.WindowHeight); if (Screen.GetWorkingArea(targetRect).IntersectsWith(targetRect)) { this.StartPosition = FormStartPosition.Manual; this.Bounds = targetRect; _lastNormalBounds = targetRect; } }
            if (settings.IsMaximized) this.WindowState = FormWindowState.Maximized;
            this.PerformLayout();
            if (splitLeftRight.ClientSize.Width > 700) { splitLeftRight.Panel1MinSize = 140; splitLeftRight.Panel2MinSize = 500; }
            if (splitCenterHorizontal.ClientSize.Width > 600) { splitCenterHorizontal.Panel1MinSize = 300; splitCenterHorizontal.Panel2MinSize = 260; }
            splitLeftRight.Panel1Collapsed = settings.IsLeftPanelCollapsed; splitCenterHorizontal.Panel2Collapsed = settings.IsRightPanelCollapsed;
            SetPaneRatios(settings.LeftPaneRatio, settings.CenterPaneRatio, settings.RightPaneRatio);
            splitLeftRight.FixedPanel = FixedPanel.Panel1; splitCenterHorizontal.FixedPanel = FixedPanel.Panel2;
        }

        private void SaveCurrentLayoutSettings()
        {
            _layoutSettings.WindowX = _lastNormalBounds.X; _layoutSettings.WindowY = _lastNormalBounds.Y; _layoutSettings.WindowWidth = _lastNormalBounds.Width; _layoutSettings.WindowHeight = _lastNormalBounds.Height; _layoutSettings.IsMaximized = (this.WindowState == FormWindowState.Maximized); _layoutSettings.Language = LocalizationService.CurrentLanguage;
            _layoutSettings.IsLeftPanelCollapsed = splitLeftRight.Panel1Collapsed; _layoutSettings.IsRightPanelCollapsed = splitCenterHorizontal.Panel2Collapsed;
            int totalW = splitLeftRight.ClientSize.Width;
            if (totalW > 0 && !splitLeftRight.Panel1Collapsed && !splitCenterHorizontal.Panel2Collapsed) { _layoutSettings.LeftPaneRatio = (double)splitLeftRight.SplitterDistance / totalW; _layoutSettings.CenterPaneRatio = (double)splitCenterHorizontal.SplitterDistance / totalW; _layoutSettings.RightPaneRatio = 1.0 - _layoutSettings.LeftPaneRatio - _layoutSettings.CenterPaneRatio; }
            _layoutSettings.Save();
        }

        private void BuildModelTreeNodes()
        {
            tvModelTree.Nodes.Clear();
            var root = new TreeNode(_kojiData.KojiName);
            foreach (var plane in _kojiData.GaisoModel.GaisoPlanes)
            {
                var planeNode = new TreeNode(plane.PlaneName);
                foreach (var surface in plane.Surfaces) planeNode.Nodes.Add(new TreeNode(surface.SurfaceName) { Tag = surface.SurfaceId });
                root.Nodes.Add(planeNode);
            }
            tvModelTree.Nodes.Add(root); root.ExpandAll();
            tvModelTree.SelectedNode = root;
        }

        private void LoadSurfaceToPropertyPanel(GaisoSurface surface)
        {
            _selectedSurface = surface;
            RefreshComponentList();
            if (surface != null) _viewport3D.SetSelectedSurface(surface.SurfaceId);
        }

        private void RefreshComponentList()
        {
            int prevIndex = lstComponents.SelectedIndex;
            lstComponents.BeginUpdate(); lstComponents.Items.Clear();
            if (_selectedSurface != null) foreach (var comp in _selectedSurface.Elements.SelectMany(e => e.MaterialComponents)) lstComponents.Items.Add($"[{comp.OrderIndex + 1}] [{comp.GetType().Name.Replace("Component", "")}] {comp.Name}");
            lstComponents.EndUpdate();
            if (prevIndex >= 0 && prevIndex < lstComponents.Items.Count) lstComponents.SelectedIndex = prevIndex; else if (lstComponents.Items.Count > 0) lstComponents.SelectedIndex = 0; else UpdatePropertyGrid(null);
        }

        private ExteriorMaterialComponent GetSelectedComponent()
        {
            if (_selectedSurface == null || lstComponents.SelectedIndex < 0) return null;
            var allComps = _selectedSurface.Elements.SelectMany(e => e.MaterialComponents).ToList();
            return (lstComponents.SelectedIndex < allComps.Count) ? allComps[lstComponents.SelectedIndex] : null;
        }

        private void UpdatePropertyGrid(ExteriorMaterialComponent comp)
        {
            propertyGridComponent.SelectedObject = comp;
            if (comp != null) lblPropertyTitle.Text = $"{LocalizationService.T("Label.PropTitleSelected")}: [{comp.GetType().Name.Replace("Component", "")}] {comp.Name}";
            else lblPropertyTitle.Text = LocalizationService.T("Label.PropTitleEmpty");
            propertyGridComponent.Refresh();
        }

        private void AddComponentToSelectedSurface(ExteriorMaterialComponent comp)
        {
            if (_selectedSurface == null) return;
            var elem = _selectedSurface.Elements.FirstOrDefault();
            if (elem == null) { elem = new ExteriorSurfaceElement { Name = "ベース要素" }; _selectedSurface.Elements.Add(elem); }
            elem.AddComponent(comp);
            RefreshComponentList(); lstComponents.SelectedIndex = lstComponents.Items.Count - 1; _viewport3D.RebuildBuildingMeshes();
        }

        private void TvModelTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is string surfaceId) { var target = _kojiData.GaisoModel.AllSurfaces.FirstOrDefault(s => s.SurfaceId == surfaceId); if (target != null) { LoadSurfaceToPropertyPanel(target); _viewport3D.ShowSurface2DPlane(target); } }
            else if (e.Node != null && e.Node.Parent == null) { _viewport3D.SwitchTo3DMode(); }
        }

        private void Viewport3D_SurfaceClicked(object sender, string surfaceId)
        {
            var target = _kojiData.GaisoModel.AllSurfaces.FirstOrDefault(s => s.SurfaceId == surfaceId);
            if (target != null)
            {
                LoadSurfaceToPropertyPanel(target);
                foreach (TreeNode r in tvModelTree.Nodes) foreach (TreeNode p in r.Nodes) foreach (TreeNode s in p.Nodes) if (s.Tag is string id && id == surfaceId) { tvModelTree.SelectedNode = s; break; }
            }
        }

        private void LstComponents_SelectedIndexChanged(object sender, EventArgs e) { UpdatePropertyGrid(GetSelectedComponent()); }

        private void PropertyGridComponent_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            var currentComp = GetSelectedComponent();
            if (currentComp != null && lstComponents.SelectedIndex >= 0) { lstComponents.Items[lstComponents.SelectedIndex] = $"[{currentComp.OrderIndex + 1}] [{currentComp.GetType().Name.Replace("Component", "")}] {currentComp.Name}"; lblPropertyTitle.Text = $"{LocalizationService.T("Label.PropTitleSelected")}: [{currentComp.GetType().Name.Replace("Component", "")}] {currentComp.Name}"; _viewport3D.RebuildBuildingMeshes(); }
        }

        private void BtnAddPanel_Click(object sender, EventArgs e) { AddComponentToSelectedSurface(new PanelComponent { Name = "外壁パネル", NameCode = "PNL-01", Material = "窯業系サイディング", Thickness = 16.0, Color = "ホワイト" }); }
        private void BtnAddMat_Click(object sender, EventArgs e) { AddComponentToSelectedSurface(new MatComponent { Name = "防水マット", NameCode = "MAT-01", Thickness = 2.0, Color = "ブラック" }); }
        private void BtnAddBoard_Click(object sender, EventArgs e) { AddComponentToSelectedSurface(new BoardComponent { Name = "防火ボード", NameCode = "BRD-01", Thickness = 12.5 }); }
        private void BtnAddFittings_Click(object sender, EventArgs e) { AddComponentToSelectedSurface(new FittingsComponent { Name = "引違い窓", FittingsType = "アルミサッシ", Width = 1650, Height = 1100 }); }

        private void BtnAddFromCatalog_Click(object sender, EventArgs e)
        {
            if (_selectedSurface == null) return;
            using (var dialog = new MaterialCatalogDialog(_catalogRepository)) { if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedCatalogItem != null) AddComponentToSelectedSurface(dialog.SelectedCatalogItem.CreateComponent()); }
        }

        private void BtnRemoveComponent_Click(object sender, EventArgs e)
        {
            var comp = GetSelectedComponent();
            if (_selectedSurface != null && comp != null) { foreach (var elem in _selectedSurface.Elements) elem.RemoveComponent(comp.Id); RefreshComponentList(); _viewport3D.RebuildBuildingMeshes(); }
        }

        private void BtnMoveUpComponent_Click(object sender, EventArgs e)
        {
            var comp = GetSelectedComponent(); if (_selectedSurface == null || comp == null) return;
            var elem = _selectedSurface.Elements.FirstOrDefault(x => x.MaterialComponents.Contains(comp));
            if (elem != null && comp.OrderIndex > 0) { int newIndex = comp.OrderIndex - 1; elem.MoveUpComponent(comp.Id); RefreshComponentList(); lstComponents.SelectedIndex = newIndex; _viewport3D.RebuildBuildingMeshes(); }
        }

        private void BtnMoveDownComponent_Click(object sender, EventArgs e)
        {
            var comp = GetSelectedComponent(); if (_selectedSurface == null || comp == null) return;
            var elem = _selectedSurface.Elements.FirstOrDefault(x => x.MaterialComponents.Contains(comp));
            if (elem != null && comp.OrderIndex < elem.MaterialComponents.Count - 1) { int newIndex = comp.OrderIndex + 1; elem.MoveDownComponent(comp.Id); RefreshComponentList(); lstComponents.SelectedIndex = newIndex; _viewport3D.RebuildBuildingMeshes(); }
        }

        private void OnRibbonButtonClick(object sender, EventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;
            string cmdKey = btn.Tag.ToString();
            switch (cmdKey)
            {
                case "Cmd.New": _kojiData = KojiData.CreateDefaultSample(); BuildModelTreeNodes(); _selectedSurface = _kojiData.GaisoModel.AllSurfaces.FirstOrDefault(); LoadSurfaceToPropertyPanel(_selectedSurface); _viewport3D.BindKojiData(_kojiData, _selectedSurface?.SurfaceId); _viewport3D.SwitchTo3DMode(); break;
                case "Cmd.Open": using (var ofd = new OpenFileDialog { Filter = "DWG / JSON (*.dwg;*.json)|*.dwg;*.json" }) { if (ofd.ShowDialog() == DialogResult.OK) { try { if (ofd.FileName.EndsWith(".dwg")) { _kojiData = new OdaDwgKojiDataReader().LoadFromDwg(ofd.FileName); _exchangeJsonPath = Path.ChangeExtension(ofd.FileName, "_gaiso.json"); } else { _kojiData = KojiData.LoadFromJson(ofd.FileName); _exchangeJsonPath = ofd.FileName; } this.Text = $"{LocalizationService.T("App.Title")} - {_kojiData.KojiName} ({_kojiData.KojiNo})"; BuildModelTreeNodes(); _selectedSurface = _kojiData.GaisoModel.AllSurfaces.FirstOrDefault(); LoadSurfaceToPropertyPanel(_selectedSurface); _viewport3D.BindKojiData(_kojiData, _selectedSurface?.SurfaceId); _viewport3D.SwitchTo3DMode(); } catch { } } } break;
                case "Cmd.Save": if (!string.IsNullOrEmpty(_exchangeJsonPath)) _kojiData.SaveToJson(_exchangeJsonPath); break;
                case "Cmd.SaveAs": using (var sfd = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = $"{_kojiData.KojiName}_gaiso.json" }) { if (sfd.ShowDialog() == DialogResult.OK) { _exchangeJsonPath = sfd.FileName; _kojiData.SaveToJson(_exchangeJsonPath); } } break;
                case "Cmd.Commit": if (!string.IsNullOrEmpty(_exchangeJsonPath)) _kojiData.SaveToJson(_exchangeJsonPath); this.DialogResult = DialogResult.OK; this.Close(); break;
                case "Cmd.Exit": this.Close(); break;
                case "Cmd.ZoomExtents": _viewport3D.ZoomExtents(); break;
                case "Cmd.Realistic": _viewport3D.SwitchTo3DMode(); break;
                case "Cmd.GridDim": _viewport3D.ToggleGridAndDimensions(); break;
                case "Cmd.LayoutStd": case "Cmd.LayoutReset": splitLeftRight.Panel1Collapsed = false; splitCenterHorizontal.Panel2Collapsed = false; SetPaneRatios(0.15, 0.65, 0.20); break;
                case "Cmd.LayoutWide": splitLeftRight.Panel1Collapsed = false; splitCenterHorizontal.Panel2Collapsed = false; SetPaneRatios(0.12, 0.72, 0.16); break;
                case "Cmd.AddPanel": BtnAddPanel_Click(sender, e); break;
                case "Cmd.AddMat": BtnAddMat_Click(sender, e); break;
                case "Cmd.AddBoard": BtnAddBoard_Click(sender, e); break;
                case "Cmd.AddFittings": BtnAddFittings_Click(sender, e); break;
                case "Cmd.Remove": BtnRemoveComponent_Click(sender, e); break;
            }
        }
    }
}
