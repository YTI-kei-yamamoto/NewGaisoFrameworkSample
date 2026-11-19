using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ymd.Common.Postal
{
    public sealed class PostalAddress
    {
        public string JisCode { get; }
        public string PostalCode { get; }
        public string PrefectureKana { get; }
        public string CityKana { get; }
        
        // 元のマスタデータ上の町域カナ（保護ベース抽出用）
        public string OriginalTownKana { get; }

        public string Prefecture { get; }
        public string City { get; }
        
        // 元のマスタデータ上の町域（保護ベース抽出用）
        public string OriginalTown { get; }
        
        // ユーザーが手動で編集したサフィックス（番地・建物名など）
        public string UserEditedSuffix { get; }

        // ユーザーが手動で編集したサフィックスのカナ（現状はサフィックスのカナ変換機構がないため、漢字と同じか空を想定）
        public string UserEditedSuffixKana { get; }

        public PostalAddress(
            string jisCode,
            string postalCode,
            string prefectureKana,
            string cityKana,
            string originalTownKana,
            string prefecture,
            string city,
            string originalTown,
            string userEditedSuffix = "",
            string userEditedSuffixKana = "")
        {
            JisCode = jisCode ?? string.Empty;
            PostalCode = postalCode ?? string.Empty;
            PrefectureKana = prefectureKana ?? string.Empty;
            CityKana = cityKana ?? string.Empty;
            OriginalTownKana = originalTownKana ?? string.Empty;
            Prefecture = prefecture ?? string.Empty;
            City = city ?? string.Empty;
            OriginalTown = originalTown ?? string.Empty;
            UserEditedSuffix = userEditedSuffix ?? string.Empty;
            UserEditedSuffixKana = userEditedSuffixKana ?? string.Empty;
        }

        /// <summary>
        /// ユーザー編集部分を結合した最終的な町域文字列を返します。
        /// </summary>
        public string Town
        {
            get
            {
                if (OriginalTown == "以下に掲載がない場合" || string.IsNullOrEmpty(OriginalTown))
                {
                    return UserEditedSuffix;
                }
                
                // 元の町域のベース部分（保護領域）に、ユーザーが編集したサフィックスを結合する
                var parts = GetAddressParts(false);
                // GetAddressParts は includePrefecture=false の場合 $"{City}{lockedTown}" を返すため、
                // 先頭の City 部分を除去して町域ベースのみを抽出
                string lockedTown = parts.LockedBase.StartsWith(City) ? parts.LockedBase.Substring(City.Length) : parts.LockedBase;
                
                return lockedTown + UserEditedSuffix;
            }
        }

        /// <summary>
        /// ユーザー編集部分（サフィックスのカナ）を結合した最終的な町域カナ文字列を返します。
        /// </summary>
        public string TownKana
        {
            get
            {
                if (OriginalTown == "以下に掲載がない場合" || string.IsNullOrEmpty(OriginalTown))
                {
                    return UserEditedSuffixKana;
                }
                
                var partsKana = GetAddressPartsKana(false);
                string lockedTownKana = partsKana.LockedBase.StartsWith(CityKana) ? partsKana.LockedBase.Substring(CityKana.Length) : partsKana.LockedBase;
                
                return lockedTownKana + UserEditedSuffixKana;
            }
        }

        /// <summary>
        /// サフィックス（番地・建物名など）と、そのカナを更新した新しい不変インスタンスを生成します。
        /// </summary>
        public PostalAddress WithEditedSuffix(string newSuffix, string newSuffixKana = "")
        {
            return new PostalAddress(
                JisCode, PostalCode, PrefectureKana, CityKana, OriginalTownKana, 
                Prefecture, City, OriginalTown, newSuffix, newSuffixKana);
        }

        public string FormattedPostalCode
        {
            get
            {
                if (PostalCode.Length != 7)
                    return PostalCode;
                return $"{PostalCode.Substring(0, 3)}-{PostalCode.Substring(3, 4)}";
            }
        }

        public string DisplayTown => OriginalTown == "以下に掲載がない場合" ? "(下記以外)" : OriginalTown;

        /// <summary>
        /// マスターの住所とユーザー編集部分（サフィックス）を結合した最終出力住所文字列を取得します。
        /// </summary>
        public string GetOutputAddress(bool includePrefecture)
        {
            var parts = GetAddressParts(includePrefecture);
            return parts.LockedBase + UserEditedSuffix;
        }

        /// <summary>
        /// 住所を「編集不可のベース部分（プレフィックス）」と「編集可能な初期値（サフィックス）」に分離します。
        /// </summary>
        public (string LockedBase, string EditableSuffix) GetAddressParts(bool includePrefecture)
        {
            if (OriginalTown == "以下に掲載がない場合" || string.IsNullOrEmpty(OriginalTown))
            {
                string baseAddr = includePrefecture ? $"{Prefecture}{City}" : City;
                return (baseAddr, string.Empty);
            }

            string lockedTown = OriginalTown;
            string editableTown = string.Empty;

            // パターン1: 「2～4丁目」などの範囲指定を分離
            var matchTilde = Regex.Match(OriginalTown, @"[０-９0-9]+[～~-][０-９0-9]+丁目$");
            if (matchTilde.Success)
            {
                lockedTown = OriginalTown.Substring(0, matchTilde.Index);
                editableTown = matchTilde.Value;
            }
            else
            {
                // パターン2: 「（１、２丁目）」「（その他）」などの末尾の括弧書きを分離
                var matchParen = Regex.Match(OriginalTown, @"[（\(].*?[）\)]$");
                if (matchParen.Success)
                {
                    lockedTown = OriginalTown.Substring(0, matchParen.Index);
                    editableTown = matchParen.Value;
                }
            }

            string lockedAddr = includePrefecture ? $"{Prefecture}{City}{lockedTown}" : $"{City}{lockedTown}";
            return (lockedAddr, editableTown);
        }
        
        /// <summary>
        /// 住所カナを「編集不可のベース部分（プレフィックス）」と「編集可能な初期値（サフィックス）」に分離します。
        /// 漢字の分離ロジックと同期してカナも分離します。
        /// </summary>
        public (string LockedBase, string EditableSuffix) GetAddressPartsKana(bool includePrefecture)
        {
            if (OriginalTown == "以下に掲載がない場合" || string.IsNullOrEmpty(OriginalTown) || string.IsNullOrEmpty(OriginalTownKana))
            {
                string baseAddrKana = includePrefecture ? $"{PrefectureKana}{CityKana}" : CityKana;
                return (baseAddrKana, string.Empty);
            }

            string lockedTownKana = OriginalTownKana;
            string editableTownKana = string.Empty;

            // 漢字側のパターンマッチ結果に基づいてカナ側を分離
            var matchTilde = Regex.Match(OriginalTown, @"[０-９0-9]+[～~-][０-９0-9]+丁目$");
            if (matchTilde.Success)
            {
                // 例: シバウラ2～4チョウメ のようなフォーマットを想定
                var matchTildeKana = Regex.Match(OriginalTownKana, @"[０-９0-9]+[～~-][０-９0-9]+チョウメ$");
                if (matchTildeKana.Success)
                {
                    lockedTownKana = OriginalTownKana.Substring(0, matchTildeKana.Index);
                    editableTownKana = matchTildeKana.Value;
                }
            }
            else
            {
                var matchParen = Regex.Match(OriginalTown, @"[（\(].*?[）\)]$");
                if (matchParen.Success)
                {
                    // 例: 括弧表記のカナ
                    var matchParenKana = Regex.Match(OriginalTownKana, @"[（\(].*?[）\)]$");
                    if (matchParenKana.Success)
                    {
                        lockedTownKana = OriginalTownKana.Substring(0, matchParenKana.Index);
                        editableTownKana = matchParenKana.Value;
                    }
                }
            }

            string lockedAddrKana = includePrefecture ? $"{PrefectureKana}{CityKana}{lockedTownKana}" : $"{CityKana}{lockedTownKana}";
            return (lockedAddrKana, editableTownKana);
        }
    }

    public class AddressListItem
    {
        public string DisplayText { get; set; }
        public string Kana { get; set; }
        public string GroupHeaderChar { get; set; }
        public string KanaRowKey { get; set; }
        public PostalAddress AddressData { get; set; }
    }

    public static class KanaIndexHelper
    {
        public static readonly string[] RowKeys = { "あ", "か", "さ", "た", "な", "は", "ま", "や", "ら", "わ", "数" };

        public static string GetHeaderHiragana(string kana, string fallbackText)
        {
            if (fallbackText == "(下記以外)") return string.Empty;
            if (string.IsNullOrWhiteSpace(kana)) return string.Empty;
            char c = kana.Trim()[0];
            if (c >= '\u30A1' && c <= '\u30F6')
            {
                char hira = (char)(c - 0x60);
                return NormalizeSeion(hira).ToString();
            }
            return string.Empty;
        }

        public static string GetRowKey(string kana, string fallbackText)
        {
            if (fallbackText == "(下記以外)") return string.Empty;
            string hira = GetHeaderHiragana(kana, fallbackText);
            if (string.IsNullOrEmpty(hira)) return "数";
            char c = hira[0];
            if ("あいうえお".IndexOf(c) >= 0) return "あ";
            if ("かきくけこ".IndexOf(c) >= 0) return "か";
            if ("さしすせそ".IndexOf(c) >= 0) return "さ";
            if ("たちつてと".IndexOf(c) >= 0) return "た";
            if ("なにぬねの".IndexOf(c) >= 0) return "な";
            if ("はひふへほ".IndexOf(c) >= 0) return "は";
            if ("まみむめも".IndexOf(c) >= 0) return "ま";
            if ("やゆよ".IndexOf(c) >= 0) return "や";
            if ("らりるれろ".IndexOf(c) >= 0) return "ら";
            if ("わをん".IndexOf(c) >= 0) return "わ";
            return "数";
        }

        private static char NormalizeSeion(char hira)
        {
            const string dakuten = "がぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽぁぃぅぇぉっゃゅょ";
            const string seion = "かきくけこさしすせそたちつてとはひふへほはひふへほあいうえおつやゆよ";
            int idx = dakuten.IndexOf(hira);
            return idx >= 0 ? seion[idx] : hira;
        }
    }
}