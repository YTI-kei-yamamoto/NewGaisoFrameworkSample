using System;
using System.Collections.Generic;

namespace Ymd.Common.Postal
{
    public sealed class PostalAddress
    {
        public string JisCode { get; }
        public string PostalCode { get; }
        public string PrefectureKana { get; }
        public string CityKana { get; }
        public string TownKana { get; }
        public string Prefecture { get; }
        public string City { get; }
        public string Town { get; }
        public string OutputAddr { get; set; }

        public PostalAddress(
            string jisCode,
            string postalCode,
            string prefectureKana,
            string cityKana,
            string townKana,
            string prefecture,
            string city,
            string town)
        {
            JisCode = jisCode ?? string.Empty;
            PostalCode = postalCode ?? string.Empty;
            PrefectureKana = prefectureKana ?? string.Empty;
            CityKana = cityKana ?? string.Empty;
            TownKana = townKana ?? string.Empty;
            Prefecture = prefecture ?? string.Empty;
            City = city ?? string.Empty;
            Town = town ?? string.Empty;
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

        public string DisplayTown => Town == "以下に掲載がない場合" ? "(下記以外)" : Town;

        public string GetOutputAddress(bool includePrefecture)
        {
            string townPart = (Town == "以下に掲載がない場合" || string.IsNullOrEmpty(Town)) ? string.Empty : Town;
            return includePrefecture ? $"{Prefecture}{City}{townPart}" : $"{City}{townPart}";
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