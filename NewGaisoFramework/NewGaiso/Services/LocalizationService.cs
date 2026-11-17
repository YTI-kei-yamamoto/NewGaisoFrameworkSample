using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Ymd.Gaiso.Simulator.Services
{
    public static class LocalizationService
    {
        public const string LangJa = "ja-JP";
        public const string LangEn = "en-US";
        public const string LangZh = "zh-CN";

        private static string _currentLanguage = LangJa;
        private static bool _isInitialized = false;

        private static readonly Dictionary<string, Dictionary<string, string>> _dictionaries =
            new Dictionary<string, Dictionary<string, string>>
            {
                [LangJa] = new Dictionary<string, string>(),
                [LangEn] = new Dictionary<string, string>(),
                [LangZh] = new Dictionary<string, string>()
            };

        public static string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value && _dictionaries.ContainsKey(value))
                {
                    _currentLanguage = value;
                    LanguageChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        public static event EventHandler LanguageChanged;

        public static void Initialize(string dbFilePath)
        {
            if (_isInitialized) return;
            string connectionString = $"Data Source={dbFilePath}";
            bool needSeed = !File.Exists(dbFilePath);

            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                if (needSeed) CreateTableAndInsertSeedData(conn);
                LoadFromDatabase(conn);
            }
            _isInitialized = true;
        }

        private static void LoadFromDatabase(SqliteConnection conn)
        {
            _dictionaries[LangJa].Clear();
            _dictionaries[LangEn].Clear();
            _dictionaries[LangZh].Clear();

            string query = "SELECT Key, Ja, En, Zh FROM Translations;";
            using (var cmd = new SqliteCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string key = reader.GetString(0);
                    if (!reader.IsDBNull(1)) _dictionaries[LangJa][key] = reader.GetString(1);
                    if (!reader.IsDBNull(2)) _dictionaries[LangEn][key] = reader.GetString(2);
                    if (!reader.IsDBNull(3)) _dictionaries[LangZh][key] = reader.GetString(3);
                }
            }
        }

        public static string T(string key)
        {
            if (!_isInitialized) return key;
            if (_dictionaries.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var val)) return val;
            if (_dictionaries[LangJa].TryGetValue(key, out var fallback)) return fallback;
            return key;
        }

        private static void CreateTableAndInsertSeedData(SqliteConnection conn)
        {
            string createTableSql = @"CREATE TABLE IF NOT EXISTS Translations (Key TEXT PRIMARY KEY, Ja TEXT NOT NULL, En TEXT, Zh TEXT);";
            using (var cmd = new SqliteCommand(createTableSql, conn)) { cmd.ExecuteNonQuery(); }

            var seedData = new[]
            {
                ("App.Title", "外装シミュレーター", "Exterior Simulator", "外装模拟器"),
                ("Tab.File", "ファイル", "File", "文件"),
                ("Tab.Home", "ホーム", "Home", "主页"),
                ("Tab.Edit", "編集", "Edit", "编辑"),
                ("Tab.View", "表示", "View", "视图"),
                ("Grp.FileOp", "ファイル操作", "File Operations", "文件操作"),
                ("Grp.System", "システム", "System", "系统"),
                ("Grp.ViewOp", "表示操作", "Navigation", "视图操作"),
                ("Grp.Style", "表示スタイル", "Visual Style", "显示样式"),
                ("Grp.Layout", "画面構成", "Layout", "界面布局"),
                ("Grp.CompEdit", "構成要素編集", "Components", "构件编辑"),
                ("Grp.Overlay", "オーバーレイ", "Overlay", "辅助标注"),
                ("Cmd.New", "新規", "New", "新建"),
                ("Cmd.Open", "開く", "Open", "打开"),
                ("Cmd.Save", "保存", "Save", "保存"),
                ("Cmd.SaveAs", "名前を付けて保存", "Save As", "另存为"),
                ("Cmd.Commit", "連携確定", "Confirm Sync", "确认同步"),
                ("Cmd.Exit", "終了", "Exit", "退出"),
                ("Cmd.Select", "選択", "Select", "选择"),
                ("Cmd.ZoomExtents", "全体表示", "Zoom Fit", "全图显示"),
                ("Cmd.Realistic", "リアル表示", "Realistic", "真实显示"),
                ("Cmd.Wireframe", "ワイヤーフレーム", "Wireframe", "线框显示"),
                ("Cmd.GridDim", "通り芯・寸法", "Grid & Dim", "轴线与尺寸"),
                ("Cmd.LayoutStd", "標準レイアウト", "Standard", "标准布局"),
                ("Cmd.LayoutWide", "3Dワイド表示", "3D Wide", "3D宽屏"),
                ("Cmd.LayoutReset", "レイアウト初期化", "Reset Layout", "重置布局"),
                ("Cmd.AddPanel", "+ パネル", "+ Panel", "+ 墙板"),
                ("Cmd.AddMat", "+ マット", "+ Mat", "+ 防水垫"),
                ("Cmd.AddBoard", "+ ボード", "+ Board", "+ 衬板"),
                ("Cmd.AddFittings", "+ 建具", "+ Fitting", "+ 门窗"),
                ("Cmd.AddCatalog", "DBカタログから追加...", "Add from DB Catalog...", "从DB目录添加..."),
                ("Cmd.Remove", "削除", "Delete", "删除"),
                ("Cmd.MoveUp", "↑上へ", "↑ Up", "↑ 上移"),
                ("Cmd.MoveDown", "↓下へ", "↓ Down", "↓ 下移"),
                ("Header.ModelTree", "モデルツリー (外装平面 / 外装面)", "Model Tree (Planes / Surfaces)", "模型树 (外装平面 / 外装面)"),
                ("Header.RightPane", "外装材構成要素 & プロパティ", "Material Components & Properties", "外装构件与属性"),
                ("Label.CompList", "構成要素一覧 (パネル・マット・ボード・建具)", "Component Layers (Panel / Mat / Board / Fitting)", "构件列表 (墙板 / 防水垫 / 衬板 / 门窗)"),
                ("Label.PropTitleEmpty", "詳細プロパティ (部材を選択してください)", "Properties (Select a component)", "详细属性 (请选择构件)"),
                ("Label.PropTitleSelected", "詳細プロパティ", "Properties", "详细属性"),
                ("Status.Ready", "準備完了（右ドラッグ:回転 / ホイール:拡大縮小 / 左クリック:面選択）", "Ready (Right-Drag: Rotate / Wheel: Zoom / Left-Click: Select)", "就绪 (右键拖动:旋转 / 滚轮:缩放 / 左键点击:选择面)"),
                ("View.Mode3D", "3D 全体パース表示", "3D Perspective View", "3D 整体透视图"),
                ("View.Mode2D", "2D XY平面表示", "2D XY Plane View", "2D XY 平面视图"),
                ("View.BackTo3D", "3D全体表示へ戻る", "Back to 3D View", "返回 3D 视图"),
                ("Dlg.CatalogTitle", "外装材・建具カタログ選択 (SQLite)", "Exterior Material & Fitting Catalog", "外装材料与门窗目录选择"),
                ("Dlg.Category", "カテゴリ分類:", "Category:", "分类筛选:"),
                ("Dlg.Keyword", "名称・型番検索:", "Search:", "名称/型号搜索:"),
                ("Dlg.SelectAdd", "選択して追加", "Select & Add", "选择并添加"),
                ("Dlg.Cancel", "キャンセル", "Cancel", "取消"),
                ("Cat.Basic", "1. 基本情報", "1. Basic Info", "1. 基本信息"),
                ("Cat.Spec", "2. 仕様", "2. Specification", "2. 规格参数"),
                ("Cat.Dim", "3. 寸法", "3. Dimensions", "3. 尺寸信息"),
                ("Prop.Id", "部材ID", "Component ID", "构件ID"),
                ("Prop.Name", "部材名称", "Component Name", "构件名称"),
                ("Prop.OrderIndex", "並び順番号", "Order Index", "排序序号"),
                ("Prop.NameCode", "型番コード", "Catalog Code", "型号代码"),
                ("Prop.Material", "材質", "Material", "材质"),
                ("Prop.Thickness", "板厚(mm)", "Thickness (mm)", "厚度 (mm)"),
                ("Prop.Color", "カラー", "Color", "颜色"),
                ("Prop.FittingsType", "建具種類", "Fitting Type", "门窗类型"),
                ("Prop.Width", "幅(W)", "Width (W)", "宽度 (W)"),
                ("Prop.Height", "高さ(H)", "Height (H)", "高度 (H)")
            };
            string insertSql = "INSERT INTO Translations (Key, Ja, En, Zh) VALUES (@Key, @Ja, @En, @Zh);";
            using (var tx = conn.BeginTransaction())
            {
                foreach (var row in seedData)
                {
                    using (var cmd = new SqliteCommand(insertSql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Key", row.Item1);
                        cmd.Parameters.AddWithValue("@Ja", row.Item2);
                        cmd.Parameters.AddWithValue("@En", row.Item3);
                        cmd.Parameters.AddWithValue("@Zh", row.Item4);
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        private readonly string _resourceKey;
        public LocalizedDisplayNameAttribute(string resourceKey) => _resourceKey = resourceKey;
        public override string DisplayName => LocalizationService.T(_resourceKey);
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class LocalizedCategoryAttribute : CategoryAttribute
    {
        private readonly string _resourceKey;
        public LocalizedCategoryAttribute(string resourceKey) : base(resourceKey) => _resourceKey = resourceKey;
        protected override string GetLocalizedString(string value) => LocalizationService.T(_resourceKey);
    }
}
