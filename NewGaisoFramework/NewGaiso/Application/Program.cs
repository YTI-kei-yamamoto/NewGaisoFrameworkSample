using System;
using System.IO;
using System.Windows.Forms;
using Ymd.Gaiso.Simulator.Models;
using Ymd.Gaiso.Simulator.Infrastructure;
using Ymd.Gaiso.Simulator.Services;

namespace Ymd.Gaiso.Simulator
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // ODA SDK ネイティブ解決用
            string odaBinDir = @"C:\ODA\Drawings_25.12\exe\vc17_amd64dll";
            if (Directory.Exists(odaBinDir))
            {
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                Environment.SetEnvironmentVariable("PATH", odaBinDir + ";" + currentPath);
            }

            // 多言語対応の翻訳データベースを初期化
            string translationsDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations.db");
            LocalizationService.Initialize(translationsDbPath);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string inputFile = args != null && args.Length > 0 ? args[0] : null;
            string exchangeJsonPath = null;
            KojiData kojiData = null;

            if (!string.IsNullOrEmpty(inputFile) && File.Exists(inputFile))
            {
                string ext = Path.GetExtension(inputFile).ToLowerInvariant();
                if (ext == ".dwg")
                {
                    try
                    {
                        var reader = new OdaDwgKojiDataReader();
                        kojiData = reader.LoadFromDwg(inputFile);
                        exchangeJsonPath = Path.ChangeExtension(inputFile, "_gaiso.json");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"DWG図面の読み込みに失敗しました。\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (ext == ".json")
                {
                    kojiData = KojiData.LoadFromJson(inputFile);
                    exchangeJsonPath = inputFile;
                }
            }

            if (kojiData == null)
            {
                kojiData = KojiData.CreateDefaultSample();
                exchangeJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleProject01_gaiso.json");
            }

            Application.Run(new MainForm(kojiData, exchangeJsonPath));
        }
    }
}
