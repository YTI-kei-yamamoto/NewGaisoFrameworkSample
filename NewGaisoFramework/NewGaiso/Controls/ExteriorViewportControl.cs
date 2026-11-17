using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ymd.Gaiso.Simulator.Models;
using Ymd.Gaiso.Simulator.Services;
using MeshGeometry3D = System.Windows.Media.Media3D.MeshGeometry3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace Ymd.Gaiso.Simulator.Controls
{
    public class ExteriorViewportControl : UserControl
    {
        private readonly HelixViewport3D _viewport;
        private readonly ModelVisual3D _buildingRootVisual;
        private readonly LinesVisual3D _selectionOutline;
        private readonly LinesVisual3D _gridAxisLines;
        private readonly LinesVisual3D _local2DAxisLines;
        private readonly Canvas _annotationCanvas;
        private readonly List<AnnotationAnchor> _annotationAnchors = new List<AnnotationAnchor>();

        // 2D/3D モード表示HUD
        private Border _modeBanner;
        private TextBlock _txtModeTitle;
        private Button _btnBackTo3D;

        private readonly Dictionary<GeometryModel3D, string> _modelToSurfaceIdMap = new Dictionary<GeometryModel3D, string>();

        private KojiData _kojiData;
        private string _selectedSurfaceId;
        private bool _showGridAndDimensions = true;
        private bool _is2DSurfaceMode = false;

        public event EventHandler<string> SurfaceClicked;

        private class AnnotationAnchor
        {
            public Point3D WorldPosition { get; set; }
            public FrameworkElement VisualElement { get; set; }
            /// <summary>
            /// 背面カリング用（値が入っている場合はカメラ方向を向いている面のみ表示する）
            /// </summary>
            public Vector3D? SurfaceNormal { get; set; }
            /// <summary>
            /// 左上基準（アンカー点をラベルの左上角に合わせる場合は true）
            /// </summary>
            public bool AlignTopLeft { get; set; }
        }

        public ExteriorViewportControl()
        {
            var rootGrid = new Grid
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(42, 92, 162),
                    Color.FromRgb(164, 198, 232),
                    90)
            };

            _viewport = new HelixViewport3D
            {
                ShowViewCube = true,
                RotateGesture = new MouseGesture(MouseAction.RightClick),
                PanGesture = new MouseGesture(MouseAction.MiddleClick)
            };
            _viewport.Camera = CreateDefault3DCamera();
            _viewport.Children.Add(new SunLight { Altitude = 48, Azimuth = 135 });

            _buildingRootVisual = new ModelVisual3D();
            _selectionOutline = new LinesVisual3D { Color = Colors.Gold, Thickness = 2.5 };
            _gridAxisLines = new LinesVisual3D { Color = Colors.Cyan, Thickness = 1.5 };
            _local2DAxisLines = new LinesVisual3D { Color = Colors.OrangeRed, Thickness = 2.0 };

            _viewport.Children.Add(_buildingRootVisual);
            _viewport.Children.Add(_selectionOutline);
            _viewport.Children.Add(_gridAxisLines);
            _viewport.Children.Add(_local2DAxisLines);

            _annotationCanvas = new Canvas { IsHitTestVisible = false };
            rootGrid.Children.Add(_viewport);
            rootGrid.Children.Add(_annotationCanvas);
            rootGrid.Children.Add(BuildModeOverlayHud());

            _viewport.MouseLeftButtonDown += (s, e) =>
            {
                if (_is2DSurfaceMode) return;
                var hits = Viewport3DHelper.FindHits(_viewport.Viewport, e.GetPosition(_viewport));
                if (hits != null && hits.Count > 0)
                {
                    foreach (var hit in hits)
                    {
                        if (hit.Model is GeometryModel3D geo && _modelToSurfaceIdMap.TryGetValue(geo, out string surfaceId))
                        {
                            SurfaceClicked?.Invoke(this, surfaceId);
                            break;
                        }
                    }
                }
            };

            _viewport.CameraChanged += (s, e) => UpdateOverlayAnnotationPositions();
            _viewport.SizeChanged += (s, e) => UpdateOverlayAnnotationPositions();
            this.Content = rootGrid;
        }

        private UIElement BuildModeOverlayHud()
        {
            _modeBanner = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(14, 14, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(215, 24, 36, 52)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(140, 120, 220, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 10, 6)
            };
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            _txtModeTitle = new TextBlock { Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            _btnBackTo3D = new Button
            {
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(8, 2, 8, 2),
                Background = new SolidColorBrush(Color.FromRgb(24, 118, 224)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Visibility = Visibility.Collapsed
            };
            _btnBackTo3D.Click += (s, e) => SwitchTo3DMode();
            stack.Children.Add(_txtModeTitle);
            stack.Children.Add(_btnBackTo3D);
            _modeBanner.Child = stack;
            return _modeBanner;
        }

        public void RefreshLocalizedTexts()
        {
            _btnBackTo3D.Content = LocalizationService.T("View.BackTo3D");
            if (_is2DSurfaceMode)
            {
                var surf = _kojiData?.GaisoModel.AllSurfaces.FirstOrDefault(s => s.SurfaceId == _selectedSurfaceId);
                if (surf != null)
                    _txtModeTitle.Text = $"{LocalizationService.T("View.Mode2D")} : {surf.SurfaceName} (W:{surf.Width * 1000:N0} × H:{surf.Height * 1000:N0} mm)";
            }
            else
            {
                _txtModeTitle.Text = LocalizationService.T("View.Mode3D");
            }
        }

        public void BindKojiData(KojiData kojiData, string selectedSurfaceId)
        {
            _kojiData = kojiData;
            _selectedSurfaceId = selectedSurfaceId;
            SwitchTo3DMode();
        }

        public void SetSelectedSurface(string surfaceId)
        {
            _selectedSurfaceId = surfaceId;
            RebuildBuildingMeshes();
        }

        /// <summary>
        /// 指定された外装面のローカルXY平面（UAxis=X, VAxis=Y）に正対した2次元平行投影ビューへ切り替え
        /// </summary>
        public void ShowSurface2DPlane(GaisoSurface surface)
        {
            if (surface == null) return;
            _selectedSurfaceId = surface.SurfaceId;
            _is2DSurfaceMode = true;

            // 2Dモード中は3D回転をロックし、中ドラッグ・右ドラッグでパン移動できるようにする
            _viewport.IsRotationEnabled = false;
            _viewport.PanGesture = new MouseGesture(MouseAction.RightClick);
            _viewport.ShowViewCube = false;
            _btnBackTo3D.Visibility = Visibility.Visible;
            RefreshLocalizedTexts();

            var uDir = new Vector3D(surface.UAxis.X, surface.UAxis.Y, surface.UAxis.Z);
            if (uDir.LengthSquared > 0) uDir.Normalize();
            var vDir = new Vector3D(surface.VAxis.X, surface.VAxis.Y, surface.VAxis.Z);
            if (vDir.LengthSquared > 0) vDir.Normalize();
            var nDir = new Vector3D(surface.Normal.X, surface.Normal.Y, surface.Normal.Z);
            if (nDir.LengthSquared > 0) nDir.Normalize();

            // 外装面の中心座標を算出
            var originPt = new Point3D(surface.Origin.X, surface.Origin.Y, surface.Origin.Z);
            var centerPt = originPt + uDir * (surface.Width * 0.5) + vDir * (surface.Height * 0.5);

            // 平行投影カメラ（OrthographicCamera）を外装面の正面（+Normal側）から -Normal 方向へ正対配置
            double viewWidth = Math.Max(surface.Width, surface.Height) * 1.45;
            _viewport.Camera = new OrthographicCamera
            {
                Position = centerPt + nDir * 20.0,
                LookDirection = -nDir * 20.0,
                UpDirection = vDir,
                Width = viewWidth
            };

            RebuildBuildingMeshes();
        }

        /// <summary>
        /// 通常の3D全体パース表示モードへ戻す
        /// </summary>
        public void SwitchTo3DMode()
        {
            _is2DSurfaceMode = false;
            _viewport.IsRotationEnabled = true;
            _viewport.RotateGesture = new MouseGesture(MouseAction.RightClick);
            _viewport.PanGesture = new MouseGesture(MouseAction.MiddleClick);
            _viewport.ShowViewCube = true;
            _btnBackTo3D.Visibility = Visibility.Collapsed;
            RefreshLocalizedTexts();

            _local2DAxisLines.Points.Clear();
            _viewport.Camera = CreateDefault3DCamera();
            RebuildBuildingMeshes();
        }

        private static PerspectiveCamera CreateDefault3DCamera() => new PerspectiveCamera
        {
            Position = new Point3D(-14.5, -16.5, 9.5),
            LookDirection = new Vector3D(14.5, 16.5, -7.2),
            UpDirection = new Vector3D(0, 0, 1),
            FieldOfView = 42
        };

        public void ToggleGridAndDimensions()
        {
            _showGridAndDimensions = !_showGridAndDimensions;
            RefreshActiveOverlay();
        }

        public void ZoomExtents()
        {
            if (_is2DSurfaceMode)
            {
                var surf = _kojiData?.GaisoModel.AllSurfaces.FirstOrDefault(s => s.SurfaceId == _selectedSurfaceId);
                if (surf != null) ShowSurface2DPlane(surf);
            }
            else
            {
                _viewport.ZoomExtents(350);
            }
        }

        public void SetWireframeMode(bool w) { }

        public void RebuildBuildingMeshes()
        {
            if (_kojiData == null) return;
            var group = new Model3DGroup();
            _selectionOutline.Points.Clear();
            _modelToSurfaceIdMap.Clear();

            foreach (var surface in _kojiData.GaisoModel.AllSurfaces)
            {
                // 2D平面表示モード中は、選択中の外装面のみを描画（背面の壁や屋根が重なるのを防止）
                if (_is2DSurfaceMode && surface.SurfaceId != _selectedSurfaceId) continue;

                var mb = new MeshBuilder(false, false);
                var p0 = new Point3D(surface.Origin.X, surface.Origin.Y, surface.Origin.Z);
                var p1 = new Point3D(p0.X + surface.Width * surface.UAxis.X, p0.Y + surface.Width * surface.UAxis.Y, p0.Z + surface.Width * surface.UAxis.Z);
                var p2 = new Point3D(p1.X + surface.Height * surface.VAxis.X, p1.Y + surface.Height * surface.VAxis.Y, p1.Z + surface.Height * surface.VAxis.Z);
                var p3 = new Point3D(p0.X + surface.Height * surface.VAxis.X, p0.Y + surface.Height * surface.VAxis.Y, p0.Z + surface.Height * surface.VAxis.Z);

                var v0 = new Vector3((float)p0.X, (float)p0.Y, (float)p0.Z);
                var v1 = new Vector3((float)p1.X, (float)p1.Y, (float)p1.Z);
                var v2 = new Vector3((float)p2.X, (float)p2.Y, (float)p2.Z);
                var v3 = new Vector3((float)p3.X, (float)p3.Y, (float)p3.Z);
                mb.AddQuad(v0, v1, v2, v3);

                var mat = MaterialTextureService.CreateFallbackMaterial();
                if (surface.Elements.FirstOrDefault()?.MaterialComponents.FirstOrDefault() is PanelComponent pc)
                    mat = MaterialTextureService.CreateHelix3DMaterial(pc);

                var geoModel = new GeometryModel3D(ToWpfMesh(mb), mat) { BackMaterial = mat };
                _modelToSurfaceIdMap[geoModel] = surface.SurfaceId;
                group.Children.Add(geoModel);

                // 建具（FittingsComponent）が構成要素に追加されている場合、2D/3D上に開口枠を描画
                var fittings = surface.Elements.SelectMany(e => e.MaterialComponents).OfType<FittingsComponent>().ToList();
                double curU = 0.8;
                foreach (var fit in fittings)
                {
                    double fw = Math.Max(0.3, fit.Width * 0.001);
                    double fh = Math.Max(0.3, fit.Height * 0.001);
                    double fv = fit.FittingsType != null && fit.FittingsType.Contains("ドア") ? 0.0 : 0.8;

                    var fp0 = p0 + new Vector3D(surface.UAxis.X, surface.UAxis.Y, surface.UAxis.Z) * curU
                                 + new Vector3D(surface.VAxis.X, surface.VAxis.Y, surface.VAxis.Z) * fv
                                 + new Vector3D(surface.Normal.X, surface.Normal.Y, surface.Normal.Z) * 0.02;
                    var fp1 = fp0 + new Vector3D(surface.UAxis.X, surface.UAxis.Y, surface.UAxis.Z) * fw;
                    var fp2 = fp1 + new Vector3D(surface.VAxis.X, surface.VAxis.Y, surface.VAxis.Z) * fh;
                    var fp3 = fp0 + new Vector3D(surface.VAxis.X, surface.VAxis.Y, surface.VAxis.Z) * fh;

                    var fmb = new MeshBuilder(false, false);
                    fmb.AddQuad(
                        new Vector3((float)fp0.X, (float)fp0.Y, (float)fp0.Z),
                        new Vector3((float)fp1.X, (float)fp1.Y, (float)fp1.Z),
                        new Vector3((float)fp2.X, (float)fp2.Y, (float)fp2.Z),
                        new Vector3((float)fp3.X, (float)fp3.Y, (float)fp3.Z));
                    var fitMat = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(46, 74, 108)));
                    group.Children.Add(new GeometryModel3D(ToWpfMesh(fmb), fitMat) { BackMaterial = fitMat });
                    curU += fw + 0.6;
                }

                if (surface.SurfaceId == _selectedSurfaceId)
                {
                    _selectionOutline.Points.Add(p0); _selectionOutline.Points.Add(p1);
                    _selectionOutline.Points.Add(p1); _selectionOutline.Points.Add(p2);
                    _selectionOutline.Points.Add(p2); _selectionOutline.Points.Add(p3);
                    _selectionOutline.Points.Add(p3); _selectionOutline.Points.Add(p0);
                }
            }
            _buildingRootVisual.Content = group;

            // メッシュ更新と同時に各面の左上ラベル・寸法・通り芯オーバーレイも最新化
            RefreshActiveOverlay();
        }

        private void RefreshActiveOverlay()
        {
            if (_kojiData == null) return;

            if (_is2DSurfaceMode)
            {
                var surf = _kojiData.GaisoModel.AllSurfaces.FirstOrDefault(s => s.SurfaceId == _selectedSurfaceId);
                if (surf != null)
                {
                    var uDir = new Vector3D(surf.UAxis.X, surf.UAxis.Y, surf.UAxis.Z);
                    if (uDir.LengthSquared > 0) uDir.Normalize();
                    var vDir = new Vector3D(surf.VAxis.X, surf.VAxis.Y, surf.VAxis.Z);
                    if (vDir.LengthSquared > 0) vDir.Normalize();
                    var nDir = new Vector3D(surf.Normal.X, surf.Normal.Y, surf.Normal.Z);
                    if (nDir.LengthSquared > 0) nDir.Normalize();

                    Rebuild2DSurfaceOverlay(surf, new Point3D(surf.Origin.X, surf.Origin.Y, surf.Origin.Z), uDir, vDir, nDir);
                }
            }
            else
            {
                Rebuild3DGridOverlay();
            }
        }

        /// <summary>
        /// 指定した外装面の左上隅に、登録されている外装材要素の名称一覧バッジを配置する
        /// </summary>
        private void AddSurfaceComponentLabelsToTopLeft(GaisoSurface surface, bool applyBackfaceCulling)
        {
            var components = surface.Elements
                .SelectMany(e => e.MaterialComponents)
                .OrderBy(c => c.OrderIndex)
                .ToList();

            if (components.Count == 0) return;

            var uDir = new Vector3D(surface.UAxis.X, surface.UAxis.Y, surface.UAxis.Z);
            if (uDir.LengthSquared > 0) uDir.Normalize();
            var vDir = new Vector3D(surface.VAxis.X, surface.VAxis.Y, surface.VAxis.Z);
            if (vDir.LengthSquared > 0) vDir.Normalize();
            var nDir = new Vector3D(surface.Normal.X, surface.Normal.Y, surface.Normal.Z);
            if (nDir.LengthSquared > 0) nDir.Normalize();

            // 面の左上隅（Origin + VAxis * Height）から少し内側へオフセットした3D座標
            double insetU = Math.Min(0.18, surface.Width * 0.05);
            double insetV = Math.Min(0.18, surface.Height * 0.05);
            var topLeftWorld = new Point3D(surface.Origin.X, surface.Origin.Y, surface.Origin.Z)
                             + vDir * (surface.Height - insetV)
                             + uDir * insetU
                             + nDir * 0.04;

            bool isSelected = (surface.SurfaceId == _selectedSurfaceId);

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // 面名称ヘッダー行
            stack.Children.Add(new TextBlock
            {
                Text = $"■ {surface.SurfaceName}",
                Foreground = isSelected ? Brushes.Gold : Brushes.LightSkyBlue,
                FontWeight = FontWeights.Bold,
                FontSize = 10.5,
                Margin = new Thickness(0, 0, 0, 2)
            });

            // 追加された外装材構成要素の名称リスト
            foreach (var comp in components)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"・[{comp.OrderIndex + 1}] {comp.Name}",
                    Foreground = Brushes.White,
                    FontSize = 10.5
                });
            }

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(205, 18, 28, 42)),
                BorderBrush = isSelected
                    ? new SolidColorBrush(Colors.Gold)
                    : new SolidColorBrush(Color.FromArgb(160, 130, 190, 255)),
                BorderThickness = new Thickness(isSelected ? 1.5 : 1.0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(7, 4, 8, 4),
                Child = stack
            };

            _annotationCanvas.Children.Add(card);
            _annotationAnchors.Add(new AnnotationAnchor
            {
                WorldPosition = topLeftWorld,
                VisualElement = card,
                SurfaceNormal = applyBackfaceCulling ? nDir : (Vector3D?)null,
                AlignTopLeft = true
            });
        }

        private void Rebuild2DSurfaceOverlay(GaisoSurface surface, Point3D origin, Vector3D uDir, Vector3D vDir, Vector3D nDir)
        {
            _gridAxisLines.Points.Clear();
            _local2DAxisLines.Points.Clear();
            _annotationCanvas.Children.Clear();
            _annotationAnchors.Clear();

            var o = origin + nDir * 0.05;
            double axisLenX = surface.Width + 0.8;
            double axisLenY = surface.Height + 0.8;
            var xEnd = o + uDir * axisLenX;
            var yEnd = o + vDir * axisLenY;

            _local2DAxisLines.Points.Add(o); _local2DAxisLines.Points.Add(xEnd);
            _local2DAxisLines.Points.Add(o); _local2DAxisLines.Points.Add(yEnd);

            AddBadgeAnnotation(o - uDir * 0.25 - vDir * 0.25, "O (0,0)", Colors.OrangeRed);
            AddBadgeAnnotation(xEnd + uDir * 0.25, "X (U)", Colors.OrangeRed);
            AddBadgeAnnotation(yEnd + vDir * 0.25, "Y (V)", Colors.OrangeRed);

            // 選択面の左上に外装材構成要素の名称一覧を表示
            AddSurfaceComponentLabelsToTopLeft(surface, applyBackfaceCulling: false);

            if (!_showGridAndDimensions)
            {
                UpdateOverlayAnnotationPositions();
                return;
            }

            // 2. パネル割付ピッチ線（仮定標準幅 910mm = 0.91m ごとの縦目地ガイド）
            double pitch = 0.91;
            for (double u = pitch; u < surface.Width; u += pitch)
            {
                _gridAxisLines.Points.Add(o + uDir * u);
                _gridAxisLines.Points.Add(o + uDir * u + vDir * surface.Height);
            }

            double dimOff = 0.45;
            var w1 = o + vDir * (surface.Height + dimOff);
            var w2 = o + uDir * surface.Width + vDir * (surface.Height + dimOff);
            _gridAxisLines.Points.Add(w1); _gridAxisLines.Points.Add(w2);
            _gridAxisLines.Points.Add(o + vDir * surface.Height); _gridAxisLines.Points.Add(w1 + vDir * 0.12);
            _gridAxisLines.Points.Add(o + uDir * surface.Width + vDir * surface.Height); _gridAxisLines.Points.Add(w2 + vDir * 0.12);
            AddBadgeAnnotation(new Point3D((w1.X + w2.X) * 0.5, (w1.Y + w2.Y) * 0.5, (w1.Z + w2.Z) * 0.5), $"W = {surface.Width * 1000:N0} mm", Colors.DeepSkyBlue);

            var h1 = o + uDir * (surface.Width + dimOff);
            var h2 = o + uDir * (surface.Width + dimOff) + vDir * surface.Height;
            _gridAxisLines.Points.Add(h1); _gridAxisLines.Points.Add(h2);
            _gridAxisLines.Points.Add(o + uDir * surface.Width); _gridAxisLines.Points.Add(h1 + uDir * 0.12);
            _gridAxisLines.Points.Add(o + uDir * surface.Width + vDir * surface.Height); _gridAxisLines.Points.Add(h2 + uDir * 0.12);
            AddBadgeAnnotation(new Point3D((h1.X + h2.X) * 0.5, (h1.Y + h2.Y) * 0.5, (h1.Z + h2.Z) * 0.5), $"H = {surface.Height * 1000:N0} mm", Colors.DeepSkyBlue);

            UpdateOverlayAnnotationPositions();
        }

        /// <summary>
        /// 3D全体表示時の通り芯（X1, Y1等）オーバーレイ構築
        /// </summary>
        private void Rebuild3DGridOverlay()
        {
            _gridAxisLines.Points.Clear();
            _local2DAxisLines.Points.Clear();
            _annotationCanvas.Children.Clear();
            _annotationAnchors.Clear();

            if (_kojiData == null) return;

            // 各外装面の左上に構成要素名称一覧を表示（カメラ側を向いている面のみ表示）
            foreach (var surface in _kojiData.GaisoModel.AllSurfaces)
            {
                AddSurfaceComponentLabelsToTopLeft(surface, applyBackfaceCulling: true);
            }

            if (!_showGridAndDimensions)
            {
                UpdateOverlayAnnotationPositions();
                return;
            }

            foreach (var axis in _kojiData.GridLines)
            {
                var isX = axis.Direction == GridDirection.X;
                var pStart = isX ? new Point3D(axis.Coordinate, -5.0, 0.05) : new Point3D(-5.0, axis.Coordinate, 0.05);
                var pEnd = isX ? new Point3D(axis.Coordinate, 5.0, 0.05) : new Point3D(5.0, axis.Coordinate, 0.05);
                _gridAxisLines.Points.Add(pStart);
                _gridAxisLines.Points.Add(pEnd);
                var balloon = new Border
                {
                    Width = 26,
                    Height = 26,
                    CornerRadius = new CornerRadius(13),
                    Background = Brushes.DarkSlateBlue,
                    BorderBrush = Brushes.Cyan,
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = axis.AxisName,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                _annotationCanvas.Children.Add(balloon);
                _annotationAnchors.Add(new AnnotationAnchor { WorldPosition = pStart, VisualElement = balloon });
            }
            UpdateOverlayAnnotationPositions();
        }

        private void AddBadgeAnnotation(Point3D worldPos, string text, Color borderColor)
        {
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(220, 22, 34, 48)),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 2, 6, 2),
                Child = new TextBlock { Text = text, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 11 }
            };
            _annotationCanvas.Children.Add(badge);
            _annotationAnchors.Add(new AnnotationAnchor { WorldPosition = worldPos, VisualElement = badge });
        }

        private void UpdateOverlayAnnotationPositions()
        {
            if (_viewport.Viewport == null) return;

            var projCam = _viewport.Viewport.Camera as ProjectionCamera;

            foreach (var anchor in _annotationAnchors)
            {
                // 3D表示時：裏側を向いている面のラベルは非表示にして視認性を保つ
                if (anchor.SurfaceNormal.HasValue && projCam != null)
                {
                    var toCamera = projCam.Position - anchor.WorldPosition;
                    if (Vector3D.DotProduct(anchor.SurfaceNormal.Value, toCamera) <= 0)
                    {
                        anchor.VisualElement.Visibility = Visibility.Collapsed;
                        continue;
                    }
                }

                var pt2d = Viewport3DHelper.Point3DtoPoint2D(_viewport.Viewport, anchor.WorldPosition);
                if (double.IsNaN(pt2d.X))
                {
                    anchor.VisualElement.Visibility = Visibility.Collapsed;
                    continue;
                }

                anchor.VisualElement.Visibility = Visibility.Visible;
                anchor.VisualElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                if (anchor.AlignTopLeft)
                {
                    // 面の左上基準点から右下方向へカードを展開
                    Canvas.SetLeft(anchor.VisualElement, pt2d.X);
                    Canvas.SetTop(anchor.VisualElement, pt2d.Y);
                }
                else
                {
                    Canvas.SetLeft(anchor.VisualElement, pt2d.X - anchor.VisualElement.DesiredSize.Width * 0.5);
                    Canvas.SetTop(anchor.VisualElement, pt2d.Y - anchor.VisualElement.DesiredSize.Height * 0.5);
                }
            }
        }

        private static MeshGeometry3D ToWpfMesh(MeshBuilder mb)
        {
            var mesh = new MeshGeometry3D();
            foreach (var p in mb.Positions) mesh.Positions.Add(new Point3D(p.X, p.Y, p.Z));
            foreach (var idx in mb.TriangleIndices) mesh.TriangleIndices.Add(idx);
            if (mb.Normals != null) foreach (var n in mb.Normals) mesh.Normals.Add(new Vector3D(n.X, n.Y, n.Z));
            if (mb.TextureCoordinates != null) foreach (var uv in mb.TextureCoordinates) mesh.TextureCoordinates.Add(new Point(uv.X, uv.Y));
            return mesh;
        }
    }
}