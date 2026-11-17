using System;
using System.IO;
using System.Text.Json;

namespace Ymd.Gaiso.Simulator.Services
{
    public class UserLayoutSettings
    {
        public int WindowX { get; set; } = -1;
        public int WindowY { get; set; } = -1;
        public int WindowWidth { get; set; } = 1440;
        public int WindowHeight { get; set; } = 920;
        public bool IsMaximized { get; set; } = false;
        public double LeftPaneRatio { get; set; } = 0.15;
        public double CenterPaneRatio { get; set; } = 0.65;
        public double RightPaneRatio { get; set; } = 0.20;
        public double TopAreaHeightRatio { get; set; } = 0.72;
        public bool IsLeftPanelCollapsed { get; set; } = false;
        public bool IsRightPanelCollapsed { get; set; } = false;
        public string Language { get; set; } = "ja-JP"; // 追加

        private static string SettingsFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YmdGaisoSimulator", "user_settings.json");

        public static UserLayoutSettings Load()
        {
            try { if (File.Exists(SettingsFilePath)) { var loaded = JsonSerializer.Deserialize<UserLayoutSettings>(File.ReadAllText(SettingsFilePath)); loaded?.NormalizeRatios(); return loaded ?? new UserLayoutSettings(); } } catch { }
            return new UserLayoutSettings();
        }

        public void Save()
        {
            try
            {
                NormalizeRatios();
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public void NormalizeRatios()
        {
            if (LeftPaneRatio < 0.05) LeftPaneRatio = 0.15;
            if (CenterPaneRatio < 0.20) CenterPaneRatio = 0.65;
            if (RightPaneRatio < 0.05) RightPaneRatio = 0.20;
            double sum = LeftPaneRatio + CenterPaneRatio + RightPaneRatio;
            LeftPaneRatio /= sum; CenterPaneRatio /= sum; RightPaneRatio /= sum;
            if (TopAreaHeightRatio < 0.30 || TopAreaHeightRatio > 0.90) TopAreaHeightRatio = 0.72;
        }
    }
}
