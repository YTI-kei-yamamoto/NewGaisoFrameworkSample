using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using Ymd.Common.Postal;

namespace WinFormsApp1
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 起動しているEXEのフルパス（例: "C:\Program Files\...\icad.exe"）
            string exeFullPath = Process.GetCurrentProcess().MainModule.FileName;
            string exeDir = Path.GetDirectoryName(exeFullPath);

            //// 現在実行中のDLL（このコードが書かれているアセンブリ）のフルパス
            //string dllFullPath = Assembly.GetExecutingAssembly().Location;
            //// DLLが存在するディレクトリパス
            //string dllDir = Path.GetDirectoryName(dllFullPath);

            // 郵便番号辞書の場所
            string mdbPath = Path.Combine(exeDir, "PostalData.mdb");
            var addr = PostalCodeDialog.ShowAddressDialog(mdbPath);
            if (addr != null)
            {
                // マスタ由来の保護された住所（例: "東京都"、"港区"、"芝浦"）
                string pref = addr.Prefecture;
                string city = addr.City;
                string town = addr.Town;
                // ユーザーが手入力・編集した部分（例: "３丁目１－１ 横河ビル"）
                string userSuffix = addr.UserEditedSuffix;
                // 全て結合した最終出力文字列（例: "東京都港区芝浦３丁目１－１ 横河ビル"）
                string fullText = addr.GetOutputAddress(includePrefecture: true);

                var addrs = $"〒{addr.FormattedPostalCode}\n{pref} ({addr.PrefectureKana})\n{city} ({addr.CityKana})\n{town} ({addr.TownKana})\n{userSuffix}\n{fullText}";
                MessageBox.Show(addrs);
            }

            //++++++++++++++++++
            //var addr2 = PostalCodeDialog.ShowDialog(_testMdbPath);
            //if (addr2 != null)
            //{
            //    var addrs = $"{addr2}";
            //    MessageBox.Show(addrs);
            //}
            //++++++++++++++++++

            //// To customize application configuration such as set high DPI settings or default font,
            //// see https://aka.ms/applicationconfiguration.
            //ApplicationConfiguration.Initialize();
            //Application.Run(new Form1());
        }
    }
}