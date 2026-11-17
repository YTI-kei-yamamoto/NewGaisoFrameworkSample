using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Ymd.Common.Postal
{
    public class AddressToZipTabControl : UserControl
    {
        private enum HierarchyLevel { Prefecture, City, Town }
        private readonly PostalDataAccess _dataAccess;
        private HierarchyLevel _currentLevel = HierarchyLevel.Prefecture;
        private string _selectedPrefecture;
        private string _selectedCity;
        private PostalAddress _selectedAddress;
        private List<AddressListItem> _currentItems = new List<AddressListItem>();

        private Panel _pnlBreadcrumb;
        private LinkLabel _lnkPrefecture;
        private LinkLabel _lnkCounty;
        private LinkLabel _lnkMunicipality;
        private Panel _pnlSelectedTownBar;
        private LinkLabel _lnkSelectedTownZip;
        private FlowLayoutPanel _pnlKanaButtons;
        private readonly Dictionary<string, Button> _kanaButtons = new Dictionary<string, Button>();
        private ListBox _lstAddress;
        private CheckBox _chkAddPrefecture;
        private RadioButton _rdoShowAll;
        private RadioButton _rdoCandidatesOnly;

        public PostalAddress CurrentSelectedAddress => _selectedAddress;
        
        private string _outputText = string.Empty;
        public string OutputAddressText => _outputText;
        
        public event EventHandler AddressConfirmed;
        public event EventHandler OutputTextChanged;

        internal AddressToZipTabControl(PostalDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
            InitializeLayout();
            LoadPrefectureLevel();
        }

        private void InitializeLayout()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = SystemColors.Control;
            this.Font = new Font("Yu Gothic UI", 9.5F);

            var mainBorderPanel = new Panel
            {
                Location = new Point(36, 12),
                Size = new Size(430, 315),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            _pnlBreadcrumb = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.White,
                Padding = new Padding(6, 4, 6, 2)
            };
            _lnkPrefecture = CreateBreadcrumbLink(6, 4);
            _lnkPrefecture.Click += (s, e) => LoadPrefectureLevel();

            _lnkCounty = CreateBreadcrumbLink(46, 24);
            _lnkCounty.Click += (s, e) => { if (!string.IsNullOrEmpty(_selectedPrefecture)) LoadCityLevel(_selectedPrefecture); };
            
            _lnkMunicipality = CreateBreadcrumbLink(86, 42);
            _lnkMunicipality.Click += (s, e) => { if (!string.IsNullOrEmpty(_selectedPrefecture)) LoadCityLevel(_selectedPrefecture); };
            
            _pnlSelectedTownBar = new Panel
            {
                Location = new Point(126, 44),
                Size = new Size(296, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(240, 240, 240),
                Visible = false
            };
            _lnkSelectedTownZip = CreateBreadcrumbLink(4, 1);
            _lnkSelectedTownZip.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_selectedPrefecture) && !string.IsNullOrEmpty(_selectedCity))
                    LoadTownLevel(_selectedPrefecture, _selectedCity);
            };
            _pnlSelectedTownBar.Controls.Add(_lnkSelectedTownZip);
            _pnlBreadcrumb.Controls.AddRange(new Control[] { _lnkPrefecture, _lnkCounty, _lnkMunicipality, _pnlSelectedTownBar });

            var separator = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.Gray };

            // 複数列段組み＋横スクロール設定
            _lstAddress = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                MultiColumn = true,
                HorizontalScrollbar = true,
                ColumnWidth = 215,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 20,
                IntegralHeight = false
            };
            
            _lstAddress.DrawItem += LstAddress_DrawItem; 
            _lstAddress.SelectedIndexChanged += LstAddress_SelectedIndexChanged; 
            _lstAddress.Click += LstAddress_Click; 
            _lstAddress.DoubleClick += LstAddress_DoubleClick;
            
            mainBorderPanel.Controls.Add(_lstAddress); 
            mainBorderPanel.Controls.Add(separator); 
            mainBorderPanel.Controls.Add(_pnlBreadcrumb);

            _pnlKanaButtons = new FlowLayoutPanel
            {
                Location = new Point(6, 72),
                Size = new Size(26, 255),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            foreach (string key in KanaIndexHelper.RowKeys)
            {
                var btn = new Button
                {
                    Text = key,
                    Size = new Size(24, 20),
                    Margin = new Padding(0, 1, 0, 0),
                    Padding = new Padding(0),
                    Font = new Font("Yu Gothic UI", 8.5F),
                    Tag = key,
                    UseVisualStyleBackColor = true
                };
                btn.Click += KanaButton_Click;
                _kanaButtons[key] = btn;
                _pnlKanaButtons.Controls.Add(btn);
            }

            _chkAddPrefecture = new CheckBox
            {
                Text = "都道府県名を付加(&T)",
                Location = new Point(12, 345),
                AutoSize = true,
                Checked = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _chkAddPrefecture.CheckedChanged += (s, e) => UpdateOutputText();

            _rdoShowAll = new RadioButton
            {
                Text = "すべての町名を表示(&W)",
                Location = new Point(165, 345),
                AutoSize = true,
                Checked = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _rdoShowAll.CheckedChanged += FilterRadio_CheckedChanged;

            _rdoCandidatesOnly = new RadioButton
            {
                Text = "変換候補のみを表示(&V)",
                Location = new Point(315, 345),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _rdoCandidatesOnly.CheckedChanged += FilterRadio_CheckedChanged;
            
            this.Controls.AddRange(new Control[] { _pnlKanaButtons, mainBorderPanel, _chkAddPrefecture, _rdoShowAll, _rdoCandidatesOnly });
        }

        private LinkLabel CreateBreadcrumbLink(int x, int y)
        {
            return new LinkLabel
            {
                Location = new Point(x, y),
                AutoSize = true,
                LinkColor = Color.Black,
                ActiveLinkColor = Color.Blue,
                VisitedLinkColor = Color.Black,
                LinkBehavior = LinkBehavior.AlwaysUnderline
            };
        }

        private void LoadPrefectureLevel()
        {
            _currentLevel = HierarchyLevel.Prefecture;
            _selectedPrefecture = null;
            _selectedCity = null;
            _selectedAddress = null;
            _lnkPrefecture.Text = "都道府県を選択してください";
            _lnkCounty.Visible = false;
            _lnkMunicipality.Visible = false;
            _pnlSelectedTownBar.Visible = false;
            _outputText = string.Empty;
            OutputTextChanged?.Invoke(this, EventArgs.Empty);
            BindListItems(_dataAccess.GetPrefectures(), false);
        }

        private void LoadCityLevel(string prefecture)
        {
            _currentLevel = HierarchyLevel.City;
            _selectedPrefecture = prefecture;
            _selectedCity = null;
            _selectedAddress = null;
            _lnkPrefecture.Text = $"{prefecture} ＜他の都道府県を選択する場合はここをクリックしてください＞";
            _lnkCounty.Visible = false;
            _lnkMunicipality.Visible = false;
            _pnlSelectedTownBar.Visible = false;
            _outputText = _chkAddPrefecture.Checked ? prefecture : string.Empty;
            OutputTextChanged?.Invoke(this, EventArgs.Empty);
            BindListItems(_dataAccess.GetCities(prefecture), true);
        }

        private void LoadTownLevel(string prefecture, string city)
        {
            _currentLevel = HierarchyLevel.Town;
            _selectedPrefecture = prefecture;
            _selectedCity = city;
            _lnkPrefecture.Text = $"{prefecture} ＜他の都道府県を選択する場合はここをクリックしてください＞";
            UpdateCityBreadcrumbs(city);
            BindListItems(_dataAccess.GetTowns(prefecture, city, _rdoCandidatesOnly.Checked), true);
            if (_currentItems.Count > 0)
            {
                _lstAddress.SelectedIndex = 0;
                ApplySelectedTown(_currentItems[0].AddressData);
            }
        }

        private void UpdateCityBreadcrumbs(string city)
        {
            int gunIndex = city.IndexOf("郡", StringComparison.Ordinal);
            if (gunIndex > 0 && gunIndex < city.Length - 1)
            {
                _lnkCounty.Text = city.Substring(0, gunIndex + 1);
                _lnkCounty.Visible = true;
                _lnkMunicipality.Text = city.Substring(gunIndex + 1);
                _lnkMunicipality.Location = new Point(86, 42);
                _lnkMunicipality.Visible = true;
            }
            else
            {
                _lnkCounty.Visible = false;
                _lnkMunicipality.Text = city;
                _lnkMunicipality.Location = new Point(46, 24);
                _lnkMunicipality.Visible = true;
            }
        }

        private void BindListItems(List<AddressListItem> items, bool assignGroupHeaders)
        {
            _currentItems = items;
            string lastHeader = null;
            int maxTextWidth = 0;

            using (Graphics g = _lstAddress.CreateGraphics())
            {
            foreach (var item in _currentItems)
            {
                    if (!assignGroupHeaders)
                    {
                        item.GroupHeaderChar = string.Empty;
                    }
                else
                {
                    string h = KanaIndexHelper.GetHeaderHiragana(item.Kana, item.DisplayText);
                        if (!string.IsNullOrEmpty(h) && h != lastHeader)
                        {
                            item.GroupHeaderChar = h;
                            lastHeader = h;
                        }
                        else
                        {
                            item.GroupHeaderChar = string.Empty;
                        }
                }

                    int textWidth = (int)Math.Ceiling(g.MeasureString(item.DisplayText, this.Font).Width);
                if (textWidth > maxTextWidth) maxTextWidth = textWidth;
            }
            }

            // 段組みを維持するため、列幅は最小140px〜最大230pxの範囲で自動調整
            _lstAddress.ColumnWidth = Math.Max(140, Math.Min(230, maxTextWidth + 36));

            _lstAddress.BeginUpdate(); 
            _lstAddress.Items.Clear(); 
            foreach (var item in _currentItems)
            {
                _lstAddress.Items.Add(item);
            }
            _lstAddress.EndUpdate();

            var availableKeys = new HashSet<string>(_currentItems.Select(x => x.KanaRowKey));
            foreach (var kv in _kanaButtons)
            {
                kv.Value.Enabled = availableKeys.Contains(kv.Key);
            }
        }

        private void LstAddress_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _currentItems.Count) return;
            
            var item = _currentItems[e.Index];
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                e.Graphics.FillRectangle(whiteBrush, e.Bounds);
            }

            // 列右端に4pxの余白を残したテキスト描画矩形
            Rectangle textBounds = new Rectangle(e.Bounds.X + 24, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 28), e.Bounds.Height);
            
            if (isSelected)
            {
                using (var selBrush = new SolidBrush(Color.FromArgb(65, 195, 255)))
                {
                    e.Graphics.FillRectangle(selBrush, textBounds);
                }
            }

            if (!string.IsNullOrEmpty(item.GroupHeaderChar))
            {
                using (var boldFont = new Font(this.Font, FontStyle.Bold))
                using (var headerBrush = new SolidBrush(Color.Black))
                {
                    e.Graphics.DrawString(item.GroupHeaderChar, boldFont, headerBrush, e.Bounds.X + 4, e.Bounds.Y + 2);
                }
            }
            
            // RectangleFを渡すことで、列幅を超える長い文字列が右隣の列と重ならないようクリッピング
            RectangleF drawRect = new RectangleF(textBounds.X + 2, textBounds.Y + 2, Math.Max(0, textBounds.Width - 2), textBounds.Height);
            using (var textBrush = new SolidBrush(Color.Black))
            using (var sf = new StringFormat(StringFormatFlags.NoWrap))
            {
                sf.Trimming = StringTrimming.None;
                e.Graphics.DrawString(item.DisplayText, this.Font, textBrush, drawRect, sf);
            }
        }

        private void LstAddress_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_currentLevel == HierarchyLevel.Town && _lstAddress.SelectedItem is AddressListItem item && item.AddressData != null)
                ApplySelectedTown(item.AddressData);
        }

        private void LstAddress_Click(object sender, EventArgs e)
        {
            if (!(_lstAddress.SelectedItem is AddressListItem item)) return;
            if (_currentLevel == HierarchyLevel.Prefecture)
                LoadCityLevel(item.DisplayText);
            else if (_currentLevel == HierarchyLevel.City)
                LoadTownLevel(_selectedPrefecture, item.DisplayText);
        }

        private void LstAddress_DoubleClick(object sender, EventArgs e)
        {
            if (_currentLevel == HierarchyLevel.Town && _selectedAddress != null)
                AddressConfirmed?.Invoke(this, EventArgs.Empty);
        }

        private void KanaButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is string rowKey)
        { 
                int targetIndex = _currentItems.FindIndex(x => x.KanaRowKey == rowKey);
                if (targetIndex >= 0)
            {
                    _lstAddress.SelectedIndex = targetIndex;
                    _lstAddress.TopIndex = targetIndex;
                }
            }
            }

        private void FilterRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (_currentLevel == HierarchyLevel.Town && !string.IsNullOrEmpty(_selectedPrefecture) && !string.IsNullOrEmpty(_selectedCity))
                LoadTownLevel(_selectedPrefecture, _selectedCity);
        }

        private void ApplySelectedTown(PostalAddress address)
        {
            _selectedAddress = address;
            _pnlSelectedTownBar.Visible = true;
            _lnkSelectedTownZip.Text = $"{address.DisplayTown}  〒{address.FormattedPostalCode}";
            UpdateOutputText();
    }

        private void UpdateOutputText()
    {
            if (_selectedAddress != null)
        {
                _outputText = _selectedAddress.GetOutputAddress(_chkAddPrefecture.Checked);
                OutputTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}