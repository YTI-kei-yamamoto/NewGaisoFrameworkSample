using System.Diagnostics;
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

            var _testMdbPath = @"C:\Ymd\Data\PostalData.mdb";
            var addr = PostalCodeDialog.ShowAddressDialog(_testMdbPath);
            if (addr != null)
            {
                Debug.WriteLine(addr.FormattedPostalCode);
                Debug.WriteLine(addr.Prefecture);
                Debug.WriteLine(addr.Town);
                Debug.WriteLine(addr.PrefectureKana);
                Debug.WriteLine(addr.TownKana);
                var addrs = $"〒{addr.FormattedPostalCode}\n{addr.Prefecture} ({addr.PrefectureKana})\n{addr.City} ({addr.CityKana})\n{addr.Town} ({addr.TownKana})\n{addr.OutputAddr}";
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