using ODASDKLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using Ymd.Gaiso.Simulator.Models;

namespace Ymd.Gaiso.Simulator.Infrastructure
{
    public class OdaDwgKojiDataReader
    {
        public KojiData LoadFromDwg(string dwgFilePath)
        {
            if (!File.Exists(dwgFilePath)) throw new FileNotFoundException("DWGファイルが見つかりません。", dwgFilePath);

            using (var srvc = new ODASDKApi())
            using (var db = new Database(false, true))
            {
                db.ReadDwgFile(dwgFilePath, FileOpenMode.OpenForReadAndAllShare, false, string.Empty);
                var kojiData = new KojiData { KojiNo = "DWG-IMP", KojiName = Path.GetFileNameWithoutExtension(dwgFilePath) };

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    var rawGridLines = new List<(Point3d Start, Point3d End)>();
                    var rawTexts = new List<(string Text, Point3d Position)>();

                    foreach (ObjectId objId in modelSpace)
                    {
                        if (tr.GetObject(objId, OpenMode.ForRead) is Entity ent)
                        {
                            if (ent is Line line && (ent.Layer.Contains("通り") || ent.Layer.Contains("GRID")))
                                rawGridLines.Add((line.StartPoint, line.EndPoint));
                            else if (ent is DBText dbText && !string.IsNullOrWhiteSpace(dbText.TextString))
                                rawTexts.Add((dbText.TextString.Trim(), dbText.Position));
                            else if (ent is MText mText && !string.IsNullOrWhiteSpace(mText.Text))
                                rawTexts.Add((mText.Text.Trim(), mText.Location));
                        }
                    }

                    kojiData.GridLines = BuildGridLineAxes(rawGridLines, rawTexts);
                    kojiData.GaisoModel.InitializeSampleBuilding(); // 簡易的にサンプル構築をフォールバック
                    tr.Commit();
                }
                return kojiData;
            }
        }

        private List<GridLineAxis> BuildGridLineAxes(List<(Point3d Start, Point3d End)> gridLines, List<(string Text, Point3d Position)> texts)
        {
            var result = new List<GridLineAxis>();
            foreach (var gl in gridLines)
            {
                bool isYAxis = Math.Abs(gl.End.X - gl.Start.X) > Math.Abs(gl.End.Y - gl.Start.Y) * 5;
                bool isXAxis = Math.Abs(gl.End.Y - gl.Start.Y) > Math.Abs(gl.End.X - gl.Start.X) * 5;
                if (!isXAxis && !isYAxis) continue;

                string expectedPrefix = isXAxis ? "X" : "Y";
                string label = texts.Where(t => t.Text.Contains(expectedPrefix) && t.Position.DistanceTo(gl.Start) < 1500)
                                    .Select(t => t.Text.Replace("(", "").Replace(")", "").Trim()).FirstOrDefault();

                if (!string.IsNullOrEmpty(label) && !result.Any(r => r.AxisName == label))
                {
                    double coord = (isXAxis ? gl.Start.X : gl.Start.Y) * 0.001;
                    result.Add(new GridLineAxis { AxisName = label, Direction = isXAxis ? GridDirection.X : GridDirection.Y, Coordinate = Math.Round(coord, 4) });
                }
            }
            return result.OrderBy(r => r.Direction).ThenBy(r => r.Coordinate).ToList();
        }
    }
}