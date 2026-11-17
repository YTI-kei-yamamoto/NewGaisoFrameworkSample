using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Ymd.Gaiso.Simulator.Models;
using Ymd.Gaiso.Simulator.Services;

namespace Ymd.Gaiso.Simulator.Models
{
    // ====================================================================
    // KojiData & グローバル定義
    // ====================================================================

    public class KojiData
    {
        public string KojiNo { get; set; } = "2026-YMD-001";
        public string KojiName { get; set; } = "SampleProject01";
        public double TotalFloorArea { get; set; } = 125.24;
        public List<GridLineAxis> GridLines { get; set; } = new List<GridLineAxis>();
        public GaisoModel GaisoModel { get; set; } = new GaisoModel();

        public void SaveToJson(string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, options), Encoding.UTF8);
        }

        public static KojiData LoadFromJson(string filePath)
        {
            var data = JsonSerializer.Deserialize<KojiData>(File.ReadAllText(filePath, Encoding.UTF8));
            return data ?? CreateDefaultSample();
        }

        public static KojiData CreateDefaultSample()
        {
            var koji = new KojiData
            {
                GridLines = new List<GridLineAxis>
                {
                    new GridLineAxis { AxisName = "X1", Direction = GridDirection.X, Coordinate = -4.55 },
                    new GridLineAxis { AxisName = "X2", Direction = GridDirection.X, Coordinate =  0.00 },
                    new GridLineAxis { AxisName = "X3", Direction = GridDirection.X, Coordinate =  4.55 },
                    new GridLineAxis { AxisName = "Y1", Direction = GridDirection.Y, Coordinate = -3.64 },
                    new GridLineAxis { AxisName = "Y2", Direction = GridDirection.Y, Coordinate =  0.00 },
                    new GridLineAxis { AxisName = "Y3", Direction = GridDirection.Y, Coordinate =  3.64 }
                }
            };
            koji.GaisoModel.InitializeSampleBuilding();
            return koji;
        }
    }

    public enum GridDirection { X, Y }

    public class GridLineAxis
    {
        public string AxisName { get; set; }
        public GridDirection Direction { get; set; }
        public double Coordinate { get; set; }
    }

    // ====================================================================
    // 幾何・ドメインモデル (GaisoModel, GaisoSurface, Vector3D等)
    // ====================================================================

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct Vector3D
    {
        [DisplayName("X成分")] public double X { get; set; }
        [DisplayName("Y成分")] public double Y { get; set; }
        [DisplayName("Z成分")] public double Z { get; set; }

        public Vector3D(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Vector3D operator +(Vector3D a, Vector3D b) => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator *(Vector3D a, double s) => new Vector3D(a.X * s, a.Y * s, a.Z * s);
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vector3D Normalize() => Length < 1e-9 ? new Vector3D(0, 0, 0) : new Vector3D(X / Length, Y / Length, Z / Length);
        public override string ToString() => $"({X:F1}, {Y:F1}, {Z:F1})";
    }

    public class OpeningRect
    {
        public string FittingMark { get; set; }
        public double U { get; set; }
        public double V { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsDoor { get; set; }
    }

    public class GaisoModel
    {
        public List<GaisoPlane> GaisoPlanes { get; set; } = new List<GaisoPlane>();
        
        [JsonIgnore]
        public IEnumerable<GaisoSurface> AllSurfaces => GaisoPlanes.SelectMany(p => p.Surfaces);

        [JsonIgnore]
        public double TotalExteriorWallArea => AllSurfaces.Where(s => s.Category == "外壁").Sum(s => s.NetArea);

        public void InitializeSampleBuilding()
        {

            double w = 9.10;
            double d = 7.28;
            double h1 = 2.90;
            double h2 = 2.80;

            var southPlane = new GaisoPlane { PlaneId = "PLN-SOUTH", PlaneName = "南面外装平面", IsWallPlane = true };
            southPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "1F-WALL-S",
                FloorGroup = "1F",
                Category = "外壁",
                SurfaceName = "外壁 (南面)",
                NetArea = 52.40,
                Origin = new Vector3D(-w / 2, -d / 2, 0),
                UAxis = new Vector3D(1, 0, 0),
                VAxis = new Vector3D(0, 0, 1),
                Normal = new Vector3D(0, -1, 0),
                Width = w,
                Height = h1,
                Openings = new List<OpeningRect>
                {
                    new OpeningRect { FittingMark = "WD-01", U = 1.2, V = 0.0, Width = 1.1, Height = 2.2, IsDoor = true },
                    new OpeningRect { FittingMark = "AW-01", U = 5.4, V = 0.8, Width = 2.4, Height = 1.6, IsDoor = false }
                }
            });
            southPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "2F-WALL-S",
                FloorGroup = "2F",
                Category = "外壁",
                SurfaceName = "2F 外壁 (南面)",
                NetArea = 54.10,
                Origin = new Vector3D(-w / 2, -d / 2, h1),
                UAxis = new Vector3D(1, 0, 0),
                VAxis = new Vector3D(0, 0, 1),
                Normal = new Vector3D(0, -1, 0),
                Width = w,
                Height = h2,
                Openings = new List<OpeningRect>
                {
                    new OpeningRect { FittingMark = "FW-01", U = 3.6, V = 1.0, Width = 0.45, Height = 1.3, IsDoor = false },
                    new OpeningRect { FittingMark = "FW-02", U = 4.3, V = 1.0, Width = 0.45, Height = 1.3, IsDoor = false },
                    new OpeningRect { FittingMark = "AW-02", U = 5.6, V = 0.4, Width = 2.2, Height = 1.9, IsDoor = false }
                }
            });

            var westPlane = new GaisoPlane { PlaneId = "PLN-WEST", PlaneName = "西面外装平面", IsWallPlane = true };
            westPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "1F-WALL-W",
                FloorGroup = "1F",
                Category = "外壁",
                SurfaceName = "外壁 (西面)",
                NetArea = 41.28,
                Origin = new Vector3D(-w / 2, d / 2, 0),
                UAxis = new Vector3D(0, -1, 0),
                VAxis = new Vector3D(0, 0, 1),
                Normal = new Vector3D(-1, 0, 0),
                Width = d,
                Height = h1,
            });
            westPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "2F-WALL-W",
                FloorGroup = "2F",
                Category = "外壁",
                SurfaceName = "2F 外壁 (西面)",
                NetArea = 39.58,
                Origin = new Vector3D(-w / 2, d / 2, h1),
                UAxis = new Vector3D(0, -1, 0),
                VAxis = new Vector3D(0, 0, 1),
                Normal = new Vector3D(-1, 0, 0),
                Width = d,
                Height = h2,
                Openings = new List<OpeningRect>
                {
                    new OpeningRect { FittingMark = "SW-01", U = 2.8, V = 1.1, Width = 1.2, Height = 1.1, IsDoor = false }
                }
            });

            var eastPlane = new GaisoPlane { PlaneId = "PLN-EAST", PlaneName = "東面外装平面", IsWallPlane = true };
            eastPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "1F-WALL-E",
                FloorGroup = "1F",
                Category = "外壁",
                SurfaceName = "外壁 (東面)",
                NetArea = 38.00,
                Origin = new Vector3D(w / 2, -d / 2, 0),
                UAxis = new Vector3D(0, 1, 0),
                VAxis = new Vector3D(0, 0, 1),
                Normal = new Vector3D(1, 0, 0),
                Width = d,
                Height = h1,
                Openings = new List<OpeningRect>
                {
                    new OpeningRect { FittingMark = "AW-03", U = 2.0, V = 0.6, Width = 1.8, Height = 1.8, IsDoor = false }
                }
            });
            eastPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "2F-WALL-E",
                FloorGroup = "2F",
                Category = "外壁",
                SurfaceName = "2F 外壁 (東面)",
                NetArea = 37.50,
                Origin = new Vector3D(w / 2, -d / 2, h1),
                UAxis = new Vector3D(0, 1, 0),
                VAxis = new Vector3D(0, 0, 1),
                Normal = new Vector3D(1, 0, 0),
                Width = d,
                Height = h2,
                Openings = new List<OpeningRect>
                {
                    new OpeningRect { FittingMark = "AW-04", U = 2.0, V = 0.6, Width = 1.8, Height = 1.8, IsDoor = false }
                }
            });

            var northPlane = new GaisoPlane { PlaneId = "PLN-NORTH", PlaneName = "北面外装平面", IsWallPlane = true };
            northPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "ALL-WALL-N",
                FloorGroup = "1F",
                Category = "外壁",
                SurfaceName = "外壁 (北面)",
                NetArea = 48.00,
                Origin = new Vector3D(w / 2, d / 2, 0),
                UAxis = new Vector3D(-1, 0, 0),
                VAxis = new Vector3D(0, 0, 1),
                Normal = new Vector3D(0, 1, 0),
                Width = w,
                Height = h1 + h2,
            });

            var balconyPlane = new GaisoPlane { PlaneId = "PLN-BALCONY", PlaneName = "バルコニー面", IsWallPlane = false };
            balconyPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "2F-BALCONY-S",
                FloorGroup = "2F",
                Category = "バルコニー",
                SurfaceName = "バルコニー手摺 (南)",
                NetArea = 8.40,
                Origin = new Vector3D(0.4, -d / 2 - 1.1, h1 - 0.1),
                UAxis = new Vector3D(1, 0, 0),
                VAxis = new Vector3D(0, 0, 1),
                Normal = new Vector3D(0, -1, 0),
                Width = 4.3,
                Height = 1.15,
            });
            balconyPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "2F-BALCONY-E",
                FloorGroup = "2F",
                Category = "バルコニー",
                SurfaceName = "バルコニー手摺 (東)",
                NetArea = 4.20,
                Origin = new Vector3D(w / 2 + 0.15, -d / 2 - 1.1, h1 - 0.1),
                UAxis = new Vector3D(0, 1, 0),
                VAxis = new Vector3D(0, 0, 1),
                Normal = new Vector3D(1, 0, 0),
                Width = 3.2,
                Height = 1.15,
            });

            var roofPlane = new GaisoPlane { PlaneId = "PLN-ROOF", PlaneName = "屋根面", IsWallPlane = false };
            roofPlane.Surfaces.Add(new GaisoSurface
            {
                SurfaceId = "ROOF-MAIN",
                FloorGroup = "屋根",
                Category = "屋根",
                SurfaceName = "大屋根 (片流れ)",
                NetArea = 78.50,
                Origin = new Vector3D(-w / 2 - 0.45, -d / 2 - 0.45, h1 + h2),
                UAxis = new Vector3D(1, 0, 0.14),
                VAxis = new Vector3D(0, 1, 0),
                Normal = new Vector3D(-0.14, 0, 1),
                Width = w + 0.9,
                Height = d + 0.9,
            });

            GaisoPlanes = new List<GaisoPlane>
            {
                southPlane, westPlane, eastPlane, northPlane, balconyPlane, roofPlane
            };
        }
    }
    //public void InitializeSampleBuilding()
    //{
    //    double w = 9.10, d = 7.28, h1 = 2.90, h2 = 2.80;

    //    var sPlane = new GaisoPlane { PlaneId = "PLN-S", PlaneName = "南面", IsWallPlane = true };
    //    sPlane.Surfaces.Add(new GaisoSurface("1F-WALL-S", "1F", "外壁", "外壁(南)", 52.40, new Vector3D(-w / 2, -d / 2, 0), new Vector3D(1, 0, 0), new Vector3D(0, 0, 1), new Vector3D(0, -1, 0), w, h1));
    //    sPlane.Surfaces.Add(new GaisoSurface("2F-WALL-S", "2F", "外壁", "2F外壁(南)", 54.10, new Vector3D(-w / 2, -d / 2, h1), new Vector3D(1, 0, 0), new Vector3D(0, 0, 1), new Vector3D(0, -1, 0), w, h2));

    //    var wPlane = new GaisoPlane { PlaneId = "PLN-W", PlaneName = "西面", IsWallPlane = true };
    //    wPlane.Surfaces.Add(new GaisoSurface("1F-WALL-W", "1F", "外壁", "外壁(西)", 41.28, new Vector3D(-w / 2, d / 2, 0), new Vector3D(0, -1, 0), new Vector3D(0, 0, 1), new Vector3D(-1, 0, 0), d, h1));

    //    var rPlane = new GaisoPlane { PlaneId = "PLN-R", PlaneName = "屋根面", IsWallPlane = false };
    //    rPlane.Surfaces.Add(new GaisoSurface("ROOF", "屋根", "屋根", "大屋根", 78.50, new Vector3D(-w / 2 - 0.45, -d / 2 - 0.45, h1 + h2), new Vector3D(1, 0, 0.14), new Vector3D(0, 1, 0), new Vector3D(-0.14, 0, 1), w + 0.9, d + 0.9));

    //    GaisoPlanes = new List<GaisoPlane> { sPlane, wPlane, rPlane };
    //}

    public class GaisoPlane
    {
        public string PlaneId { get; set; }
        public string PlaneName { get; set; }
        public bool IsWallPlane { get; set; }
        public List<GaisoSurface> Surfaces { get; set; } = new List<GaisoSurface>();
    }

    public class GaisoSurface
    {
        public string SurfaceId { get; set; }
        public string FloorGroup { get; set; }
        public string Category { get; set; }
        public string SurfaceName { get; set; }
        public double NetArea { get; set; }

        public Vector3D Origin { get; set; }
        public Vector3D UAxis { get; set; }
        public Vector3D VAxis { get; set; }
        public Vector3D Normal { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public List<OpeningRect> Openings { get; set; } = new List<OpeningRect>();
        public List<ExteriorSurfaceElement> Elements { get; set; } = new List<ExteriorSurfaceElement>();

        public bool IsSpecialDetail { get; set; }
        public string SpecialDetailNote { get; set; } = string.Empty;

        public GaisoSurface()
        {
           // SurfaceId = Guid.NewGuid().ToString();
        }

        public GaisoSurface(string id, string floor, string cat, string name, double area, Vector3D org, Vector3D u, Vector3D v, Vector3D n, double w, double h)
        {
            SurfaceId = id; FloorGroup = floor; Category = cat; SurfaceName = name; NetArea = area;
            Origin = org; UAxis = u; VAxis = v; Normal = n; Width = w; Height = h;
            
            var baseElem = new ExteriorSurfaceElement { Name = "ベース要素" };
            baseElem.AddComponent(new PanelComponent { Name = "標準サイディング", NameCode = "PNL-01", Color = "ホワイト", Thickness = 16.0, Material = "窯業" });
            Elements.Add(baseElem);
        }
    }

    // ====================================================================
    // 外装材構成要素 (多言語属性付き)
    // ====================================================================

    public class ExteriorSurfaceElement
    {
        public string Id { get; private set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public int OrderIndex { get; set; }

        [JsonInclude]
        private List<ExteriorMaterialComponent> _materialComponents = new List<ExteriorMaterialComponent>();

        [JsonIgnore]
        public IReadOnlyList<ExteriorMaterialComponent> MaterialComponents => _materialComponents.OrderBy(c => c.OrderIndex).ToList().AsReadOnly();

        public void AddComponent(ExteriorMaterialComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            component.OrderIndex = _materialComponents.Count;
            _materialComponents.Add(component);
        }

        public bool RemoveComponent(string componentId)
        {
            int removed = _materialComponents.RemoveAll(c => c.Id == componentId);
            if (removed > 0) { Reindex(); return true; }
            return false;
        }

        public void MoveUpComponent(string componentId)
        {
            var comp = _materialComponents.FirstOrDefault(c => c.Id == componentId);
            if (comp == null || comp.OrderIndex <= 0) return;
            var prevComp = _materialComponents.First(c => c.OrderIndex == comp.OrderIndex - 1);
            prevComp.OrderIndex++;
            comp.OrderIndex--;
        }

        public void MoveDownComponent(string componentId)
        {
            var comp = _materialComponents.FirstOrDefault(c => c.Id == componentId);
            if (comp == null || comp.OrderIndex >= _materialComponents.Count - 1) return;
            var nextComp = _materialComponents.First(c => c.OrderIndex == comp.OrderIndex + 1);
            nextComp.OrderIndex--;
            comp.OrderIndex++;
        }

        private void Reindex()
        {
            var sorted = _materialComponents.OrderBy(c => c.OrderIndex).ToList();
            for (int i = 0; i < sorted.Count; i++) sorted[i].OrderIndex = i;
        }
    }

    [JsonDerivedType(typeof(PanelComponent), typeDiscriminator: "Panel")]
    [JsonDerivedType(typeof(MatComponent), typeDiscriminator: "Mat")]
    [JsonDerivedType(typeof(BoardComponent), typeDiscriminator: "Board")]
    [JsonDerivedType(typeof(FittingsComponent), typeDiscriminator: "Fittings")]
    public abstract class ExteriorMaterialComponent
    {
        [LocalizedCategory("Cat.Basic"), LocalizedDisplayName("Prop.Id"), ReadOnly(true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [LocalizedCategory("Cat.Basic"), LocalizedDisplayName("Prop.Name")]
        public string Name { get; set; }

        [LocalizedCategory("Cat.Basic"), LocalizedDisplayName("Prop.OrderIndex"), ReadOnly(true)]
        public int OrderIndex { get; set; }
    }

    public class PanelComponent : ExteriorMaterialComponent
    {
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.NameCode")] public string NameCode { get; set; }
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.Material")] public string Material { get; set; }
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.Thickness")] public double Thickness { get; set; }
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.Color")] public string Color { get; set; }
    }

    public class MatComponent : ExteriorMaterialComponent
    {
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.NameCode")] public string NameCode { get; set; }
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.Thickness")] public double Thickness { get; set; }
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.Color")] public string Color { get; set; }
    }

    public class BoardComponent : ExteriorMaterialComponent
    {
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.NameCode")] public string NameCode { get; set; }
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.Thickness")] public double Thickness { get; set; }
    }

    public class FittingsComponent : ExteriorMaterialComponent
    {
        [LocalizedCategory("Cat.Spec"), LocalizedDisplayName("Prop.FittingsType")] public string FittingsType { get; set; }
        [LocalizedCategory("Cat.Dim"), LocalizedDisplayName("Prop.Width")] public double Width { get; set; }
        [LocalizedCategory("Cat.Dim"), LocalizedDisplayName("Prop.Height")] public double Height { get; set; }
    }

    // ====================================================================
    // カタログマスタ & SQLiteリポジトリ
    // ====================================================================

    public enum MaterialCategory { Panel, Mat, Board, Fittings }

    public class MaterialCatalogItem
    {
        public string CatalogCode { get; set; }
        public string Name { get; set; }
        public MaterialCategory Category { get; set; }
        public string Material { get; set; }
        public double Thickness { get; set; }
        public string Color { get; set; }
        public string FittingsType { get; set; }
        public double DefaultWidth { get; set; }
        public double DefaultHeight { get; set; }
        public string Options { get; set; }

        public ExteriorMaterialComponent CreateComponent()
        {
            switch (Category)
            {
                case MaterialCategory.Panel: return new PanelComponent { Name = this.Name, NameCode = this.CatalogCode, Material = this.Material, Thickness = this.Thickness, Color = this.Color };
                case MaterialCategory.Mat: return new MatComponent { Name = this.Name, NameCode = this.CatalogCode, Thickness = this.Thickness, Color = this.Color };
                case MaterialCategory.Board: return new BoardComponent { Name = this.Name, NameCode = this.CatalogCode, Thickness = this.Thickness };
                case MaterialCategory.Fittings: return new FittingsComponent { Name = this.Name, FittingsType = this.FittingsType, Width = this.DefaultWidth, Height = this.DefaultHeight };
                default: throw new InvalidOperationException("未対応");
            }
        }
    }

    public interface IMaterialCatalogRepository
    {
        List<MaterialCatalogItem> GetAllCatalogItems();
        List<MaterialCatalogItem> GetCatalogItemsByCategory(MaterialCategory category);
    }

    public class SqliteMaterialCatalogRepository : IMaterialCatalogRepository
    {
        private readonly string _connectionString;

        public SqliteMaterialCatalogRepository(string dbFilePath)
        {
            _connectionString = $"Data Source={dbFilePath}";
            InitializeDatabase(dbFilePath);
        }

        private void InitializeDatabase(string dbFilePath)
        {
            bool needSeed = !File.Exists(dbFilePath);
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string createTableSql = @"CREATE TABLE IF NOT EXISTS MaterialCatalog (CatalogCode TEXT PRIMARY KEY, Name TEXT NOT NULL, Category TEXT NOT NULL, Material TEXT, Thickness REAL, Color TEXT, FittingsType TEXT, DefaultWidth REAL, DefaultHeight REAL, Options TEXT);";
                using (var command = new SqliteCommand(createTableSql, connection)) { command.ExecuteNonQuery(); }

                if (needSeed)
                {
                    string insertSql = "INSERT INTO MaterialCatalog (CatalogCode, Name, Category, Material, Thickness, Color, FittingsType, DefaultWidth, DefaultHeight, Options) VALUES (@Code, @Name, @Category, @Material, @Thickness, @Color, @FittingsType, @Width, @Height, @Options);";
                    var seedItems = new List<MaterialCatalogItem>
                    {
                        new MaterialCatalogItem { CatalogCode = "PNL-CER-01", Name = "光触媒サイディング16", Category = MaterialCategory.Panel, Material = "窯業系サイディング", Thickness = 16.0, Color = "ストーンホワイト" },
                        new MaterialCatalogItem { CatalogCode = "PNL-GL-02", Name = "ガルバリウム角波パネル", Category = MaterialCategory.Panel, Material = "ガルバリウム鋼板", Thickness = 15.0, Color = "マットブラック" },
                        new MaterialCatalogItem { CatalogCode = "MAT-WRAP-01", Name = "透湿防水シートType-A", Category = MaterialCategory.Mat, Thickness = 0.2, Color = "ホワイト" },
                        new MaterialCatalogItem { CatalogCode = "BRD-STRUCT-01", Name = "構造用耐火ボード", Category = MaterialCategory.Board, Thickness = 12.5, Color = "グレー" },
                        new MaterialCatalogItem { CatalogCode = "FIT-WIN-01", Name = "樹脂複層引違い窓", Category = MaterialCategory.Fittings, FittingsType = "引違い窓", DefaultWidth = 1650, DefaultHeight = 1100, Options = "Low-Eペアガラス" },
                        new MaterialCatalogItem { CatalogCode = "FIT-DOOR-01", Name = "高断熱スチールドア", Category = MaterialCategory.Fittings, FittingsType = "開き戸", DefaultWidth = 920, DefaultHeight = 2330, Options = "電子錠・採光窓付" }
                    };
                    using (var transaction = connection.BeginTransaction())
                    {
                        foreach (var item in seedItems)
                        {
                            using (var command = new SqliteCommand(insertSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Code", item.CatalogCode);
                                command.Parameters.AddWithValue("@Name", item.Name);
                                command.Parameters.AddWithValue("@Category", item.Category.ToString());
                                command.Parameters.AddWithValue("@Material", (object)item.Material ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Thickness", item.Thickness);
                                command.Parameters.AddWithValue("@Color", (object)item.Color ?? DBNull.Value);
                                command.Parameters.AddWithValue("@FittingsType", (object)item.FittingsType ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Width", item.DefaultWidth);
                                command.Parameters.AddWithValue("@Height", item.DefaultHeight);
                                command.Parameters.AddWithValue("@Options", (object)item.Options ?? DBNull.Value);
                                command.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                }
            }
        }

        public List<MaterialCatalogItem> GetAllCatalogItems()
        {
            var results = new List<MaterialCatalogItem>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand("SELECT * FROM MaterialCatalog ORDER BY Category, CatalogCode;", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) results.Add(MapRow(reader));
                }
            }
            return results;
        }

        public List<MaterialCatalogItem> GetCatalogItemsByCategory(MaterialCategory category)
        {
            var results = new List<MaterialCatalogItem>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand("SELECT * FROM MaterialCatalog WHERE Category = @Category ORDER BY CatalogCode;", connection))
                {
                    command.Parameters.AddWithValue("@Category", category.ToString());
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) results.Add(MapRow(reader));
                    }
                }
            }
            return results;
        }

        private MaterialCatalogItem MapRow(SqliteDataReader reader)
        {
            return new MaterialCatalogItem
            {
                CatalogCode = reader.GetString(reader.GetOrdinal("CatalogCode")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Category = (MaterialCategory)Enum.Parse(typeof(MaterialCategory), reader.GetString(reader.GetOrdinal("Category"))),
                Material = reader.IsDBNull(reader.GetOrdinal("Material")) ? null : reader.GetString(reader.GetOrdinal("Material")),
                Thickness = reader.IsDBNull(reader.GetOrdinal("Thickness")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("Thickness")),
                Color = reader.IsDBNull(reader.GetOrdinal("Color")) ? null : reader.GetString(reader.GetOrdinal("Color")),
                FittingsType = reader.IsDBNull(reader.GetOrdinal("FittingsType")) ? null : reader.GetString(reader.GetOrdinal("FittingsType")),
                DefaultWidth = reader.IsDBNull(reader.GetOrdinal("DefaultWidth")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("DefaultWidth")),
                DefaultHeight = reader.IsDBNull(reader.GetOrdinal("DefaultHeight")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("DefaultHeight")),
                Options = reader.IsDBNull(reader.GetOrdinal("Options")) ? null : reader.GetString(reader.GetOrdinal("Options"))
            };
        }
    }
}
