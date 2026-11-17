using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Ymd.Gaiso.Simulator.Controls;
using Ymd.Gaiso.Simulator.Services;

namespace Ymd.Gaiso.Simulator
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel pnlTopHeader;
        private FlowLayoutPanel flpRibbonTabs;
        private Panel pnlRibbonContentHost;
        private ComboBox cmbLanguage;
        private readonly Dictionary<string, Button> _ribbonTabButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, FlowLayoutPanel> _ribbonTabPanels = new Dictionary<string, FlowLayoutPanel>();
        private readonly List<(Label LabelCtrl, string Key)> _ribbonGroupLabels = new List<(Label, string)>();
        private readonly List<(Button BtnCtrl, string Key)> _ribbonCommandButtons = new List<(Button, string)>();

        private SplitContainer splitLeftRight;
        private SplitContainer splitCenterHorizontal;
        private TreeView tvModelTree;
        private ElementHost elementHost3D;
        private ExteriorViewportControl _viewport3D;
        private Label lblModelTreeHeader;
        private Label lblRightPaneHeader;
        private Label lblComponentsTitle;
        private ListBox lstComponents;
        private FlowLayoutPanel flowComponentsButtons;
        private Button btnAddPanel;
        private Button btnAddMat;
        private Button btnAddBoard;
        private Button btnAddFittings;
        private Button btnAddFromCatalog;
        private Button btnRemoveComponent;
        private Button btnMoveUpComponent;
        private Button btnMoveDownComponent;
        private Label lblPropertyTitle;
        private PropertyGrid propertyGridComponent;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatusText;
        private ToolStripStatusLabel lblCoordsText;
        private ToolStripStatusLabel lblScaleText;

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.Size = new Size(1440, 920);
            this.MinimumSize = new Size(1024, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Yu Gothic UI", 9f);
            this.BackColor = Color.FromArgb(240, 243, 247);

            BuildTopRibbon();
            BuildMainLayout();
            BuildBottomStatusBar();
        }

        private void BuildTopRibbon()
        {
            pnlTopHeader = new Panel { Dock = DockStyle.Top, Height = 112, BackColor = Color.FromArgb(245, 247, 250) };
            flpRibbonTabs = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(232, 237, 244), Padding = new Padding(0), Margin = new Padding(0) };
            pnlRibbonContentHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            string[] tabs = { "Tab.File", "Tab.Home", "Tab.Edit", "Tab.View" };
            foreach (var tKey in tabs)
            {
                var btnTab = new Button { Tag = tKey, FlatStyle = FlatStyle.Flat, Height = 30, Width = (tKey == "Tab.File") ? 76 : 90, Margin = new Padding(0), Cursor = Cursors.Hand };
                btnTab.FlatAppearance.BorderSize = 0; btnTab.Click += (s, e) => SelectRibbonTab(tKey);
                flpRibbonTabs.Controls.Add(btnTab); _ribbonTabButtons[tKey] = btnTab;
                var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 2), Visible = false };
                pnlRibbonContentHost.Controls.Add(flow); _ribbonTabPanels[tKey] = flow;
            }

            cmbLanguage = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 115, Height = 26, Margin = new Padding(16, 3, 4, 0), Font = new Font("Yu Gothic UI", 8.5f) };
            cmbLanguage.Items.AddRange(new object[] { "日本語", "English", "简体中文" });
            cmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;
            flpRibbonTabs.Controls.Add(cmbLanguage);

            AddRibbonGroup("Tab.File", "Grp.FileOp", new[] { "Cmd.New", "Cmd.Open", "Cmd.Save", "Cmd.SaveAs" }, null);
            AddRibbonGroup("Tab.File", "Grp.System", new[] { "Cmd.Commit", "Cmd.Exit" }, null);
            AddRibbonGroup("Tab.Home", "Grp.ViewOp", new[] { "Cmd.Select", "Cmd.ZoomExtents" }, "Cmd.Select");
            AddRibbonGroup("Tab.Home", "Grp.Style", new[] { "Cmd.Realistic", "Cmd.Wireframe", "Cmd.GridDim" }, "Cmd.Realistic");
            AddRibbonGroup("Tab.Home", "Grp.Layout", new[] { "Cmd.LayoutStd", "Cmd.LayoutWide" }, null);
            AddRibbonGroup("Tab.Edit", "Grp.CompEdit", new[] { "Cmd.AddPanel", "Cmd.AddMat", "Cmd.AddBoard", "Cmd.AddFittings", "Cmd.Remove" }, null);
            AddRibbonGroup("Tab.View", "Grp.Overlay", new[] { "Cmd.GridDim", "Cmd.ZoomExtents" }, null);
            AddRibbonGroup("Tab.View", "Grp.Layout", new[] { "Cmd.LayoutStd", "Cmd.LayoutWide", "Cmd.LayoutReset" }, null);

            pnlTopHeader.Controls.Add(pnlRibbonContentHost);
            pnlTopHeader.Controls.Add(flpRibbonTabs);
            this.Controls.Add(pnlTopHeader);

            SelectRibbonTab("Tab.Home");
        }

        private void AddRibbonGroup(string tabKey, string groupKey, string[] btnKeys, string activeKey)
        {
            if (!_ribbonTabPanels.TryGetValue(tabKey, out var flowPanel)) return;
            var group = new Panel { Height = 74, Width = btnKeys.Length * 66 + 12, Margin = new Padding(0, 0, 6, 0) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56, WrapContents = false };
            foreach (var bKey in btnKeys)
            {
                bool isActive = (bKey == activeKey);
                Color iconColor = isActive ? Color.FromArgb(16, 102, 204) : Color.FromArgb(55, 65, 80);
                var btn = new Button { Tag = bKey, Width = 62, Height = 54, FlatStyle = FlatStyle.Flat, Image = CreateRibbonIcon(bKey, iconColor), TextImageRelation = TextImageRelation.ImageAboveText, TextAlign = ContentAlignment.BottomCenter };
                btn.FlatAppearance.BorderColor = isActive ? Color.FromArgb(120, 180, 245) : Color.FromArgb(230, 234, 240);
                btn.Click += OnRibbonButtonClick;
                flow.Controls.Add(btn); _ribbonCommandButtons.Add((btn, bKey));
            }
            var lbl = new Label { Dock = DockStyle.Bottom, Height = 16, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray, Font = new Font("Yu Gothic UI", 7.5f) };
            group.Controls.Add(flow); group.Controls.Add(lbl); flowPanel.Controls.Add(group);
            _ribbonGroupLabels.Add((lbl, groupKey));
        }

        private static Bitmap CreateRibbonIcon(string commandKey, Color color)
        {
            string glyph = commandKey switch { "Cmd.New" => "\uE8A5", "Cmd.Open" => "\uE8E5", "Cmd.Save" => "\uE74E", "Cmd.SaveAs" => "\uE792", "Cmd.Commit" => "\uE73E", "Cmd.Exit" => "\uE7E8", "Cmd.Select" => "\uE8B0", "Cmd.ZoomExtents" => "\uE740", "Cmd.Realistic" => "\uF158", "Cmd.Wireframe" => "\uE80A", "Cmd.GridDim" => "\uE809", "Cmd.LayoutStd" => "\uE737", "Cmd.LayoutWide" => "\uE737", "Cmd.LayoutReset" => "\uE737", "Cmd.AddPanel" => "\uE710", "Cmd.AddMat" => "\uE710", "Cmd.AddBoard" => "\uE710", "Cmd.AddFittings" => "\uE710", "Cmd.Remove" => "\uE74D", _ => "\uE713" };
            return CreateSegoeIcon(glyph, color, 20);
        }

        private static Bitmap CreateSegoeIcon(string glyph, Color color, int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                using (var font = new Font("Segoe MDL2 Assets", size * 0.72f, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(color))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(glyph, font, brush, new RectangleF(0, 0, size, size), sf);
                }
            }
            return bmp;
        }

        private void BuildMainLayout()
        {
            splitLeftRight = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterWidth = 5, BackColor = Color.FromArgb(216, 222, 230) };
            splitCenterHorizontal = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterWidth = 5 };

            var pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            lblModelTreeHeader = new Label { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(236, 240, 246), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Yu Gothic UI", 9f, FontStyle.Bold), Padding = new Padding(8,0,8,0) };
            tvModelTree = new TreeView { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Yu Gothic UI", 9f), ItemHeight = 22 };
            tvModelTree.AfterSelect += TvModelTree_AfterSelect;
            pnlLeft.Controls.Add(tvModelTree); pnlLeft.Controls.Add(lblModelTreeHeader);
            splitLeftRight.Panel1.Controls.Add(pnlLeft);

            _viewport3D = new ExteriorViewportControl();
            _viewport3D.SurfaceClicked += Viewport3D_SurfaceClicked;
            elementHost3D = new ElementHost { Dock = DockStyle.Fill, Child = _viewport3D };
            splitCenterHorizontal.Panel1.Controls.Add(elementHost3D);

            var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            lblRightPaneHeader = new Label { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(236, 240, 246), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Yu Gothic UI", 9f, FontStyle.Bold), Padding = new Padding(8,0,8,0) };
            
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(6) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 36F)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 64F));

            lblComponentsTitle = new Label { Font = new Font("Yu Gothic UI", 8.8f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
            lstComponents = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, Font = new Font("Yu Gothic UI", 9f) };
            lstComponents.SelectedIndexChanged += LstComponents_SelectedIndexChanged;

            flowComponentsButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 2, 0, 2) };
            btnAddPanel = new Button { Size = new Size(66, 26) }; btnAddMat = new Button { Size = new Size(66, 26) }; btnAddBoard = new Button { Size = new Size(66, 26) }; btnAddFittings = new Button { Size = new Size(66, 26) };
            btnAddFromCatalog = new Button { Size = new Size(138, 26), BackColor = Color.FromArgb(222, 238, 255), FlatStyle = FlatStyle.Flat }; btnAddFromCatalog.FlatAppearance.BorderColor = Color.FromArgb(120, 180, 245);
            btnRemoveComponent = new Button { Size = new Size(52, 26) }; btnMoveUpComponent = new Button { Size = new Size(60, 26) }; btnMoveDownComponent = new Button { Size = new Size(60, 26) };
            
            btnAddPanel.Click += BtnAddPanel_Click; btnAddMat.Click += BtnAddMat_Click; btnAddBoard.Click += BtnAddBoard_Click; btnAddFittings.Click += BtnAddFittings_Click; btnAddFromCatalog.Click += BtnAddFromCatalog_Click; btnRemoveComponent.Click += BtnRemoveComponent_Click; btnMoveUpComponent.Click += BtnMoveUpComponent_Click; btnMoveDownComponent.Click += BtnMoveDownComponent_Click;
            flowComponentsButtons.Controls.AddRange(new Control[] { btnAddPanel, btnAddMat, btnAddBoard, btnAddFittings, btnAddFromCatalog, btnRemoveComponent, btnMoveUpComponent, btnMoveDownComponent });

            lblPropertyTitle = new Label { Font = new Font("Yu Gothic UI", 8.8f, FontStyle.Bold), ForeColor = Color.DarkSlateBlue, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
            propertyGridComponent = new PropertyGrid { Dock = DockStyle.Fill, ToolbarVisible = true, PropertySort = PropertySort.Categorized };
            propertyGridComponent.PropertyValueChanged += PropertyGridComponent_PropertyValueChanged;

            layout.Controls.Add(lblComponentsTitle, 0, 0); layout.Controls.Add(lstComponents, 0, 1); layout.Controls.Add(flowComponentsButtons, 0, 2); layout.Controls.Add(lblPropertyTitle, 0, 3); layout.Controls.Add(propertyGridComponent, 0, 4);
            pnlRight.Controls.Add(layout); pnlRight.Controls.Add(lblRightPaneHeader);
            splitCenterHorizontal.Panel2.Controls.Add(pnlRight);

            splitLeftRight.Panel2.Controls.Add(splitCenterHorizontal);
            this.Controls.Add(splitLeftRight);
            splitLeftRight.BringToFront();
        }

        private void BuildBottomStatusBar()
        {
            statusStrip = new StatusStrip { BackColor = Color.FromArgb(234, 238, 244), SizingGrip = false };
            lblStatusText = new ToolStripStatusLabel { Width = 440, TextAlign = ContentAlignment.MiddleLeft };
            lblCoordsText = new ToolStripStatusLabel("単位: m (WCS)") { Width = 140 };
            lblScaleText = new ToolStripStatusLabel("3Dエンジン: HelixToolkit.Wpf / ODA SDK 対応") { Spring = true, TextAlign = ContentAlignment.MiddleRight };
            statusStrip.Items.Add(lblStatusText); statusStrip.Items.Add(new ToolStripSeparator()); statusStrip.Items.Add(lblCoordsText); statusStrip.Items.Add(lblScaleText);
            this.Controls.Add(statusStrip);
        }
    }
}
