using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Windows.Forms;

namespace Ymd.Common.Postal
{
    public static class PostalCodeDialog
    {
        /// <summary>
        /// 郵便番号検索ダイアログを表示し、選択された住所文字列を返します。
        /// </summary>
        /// <param name="mdbPath">PostData.mdbのフルパス</param>
        /// <param name="includePrefecture">デフォルトで都道府県を付加するか</param>
        /// <returns>選択された住所文字列（キャンセル時はnull）</returns>
        public static string ShowDialog(string mdbPath, bool includePrefecture = true)
        {
            if (!File.Exists(mdbPath))
            {
                throw new FileNotFoundException("郵便番号データベースが見つかりません。", mdbPath);
            }
            using (var form = new PostalSearchForm(mdbPath))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    return form.SelectedOutputText;
                }
            }
            return null;
        }

        /// <summary>
        /// 郵便番号検索ダイアログを表示し、選択された住所オブジェクトを返します。
        /// </summary>
        /// <param name="mdbPath">PostData.mdbのフルパス</param>
        /// <returns>選択された住所オブジェクト（キャンセル時はnull）</returns>
        public static PostalAddress ShowAddressDialog(string mdbPath)
        {
            if (!File.Exists(mdbPath))
            {
                throw new FileNotFoundException("郵便番号データベースが見つかりません。", mdbPath);
            }
            using (var form = new PostalSearchForm(mdbPath))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    return form.SelectedAddressData;
                }
            }
            return null;
        }
    }
}