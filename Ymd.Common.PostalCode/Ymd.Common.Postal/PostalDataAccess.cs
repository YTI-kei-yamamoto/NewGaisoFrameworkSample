using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace Ymd.Common.Postal
{
    public class PostalDataAccess
    {
        private readonly string _connectionString;

        public PostalDataAccess(string mdbPath)
        {
            _connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={mdbPath};"; // Persist Security Info=False;";
        }

        public List<PostalAddress> SearchByPostalCode(string postalCode, bool ignoreBranch)
        {
            var results = new List<PostalAddress>();
            string searchParam = ignoreBranch ? $"{postalCode}%" : postalCode;
            string sql = @"
                SELECT [全国地方公共団体コード], [郵便番号], [都道府県カナ], [市区町村カナ], [町域カナ],
                       [都道府県], [市区町村], [町域]
                FROM [T_PostalCode]
                WHERE [郵便番号] LIKE @Code
                ORDER BY [全国地方公共団体コード], [郵便番号]";

            using (var conn = new OleDbConnection(_connectionString))
            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Code", searchParam);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new PostalAddress(
                            reader["全国地方公共団体コード"].ToString(),
                            reader["郵便番号"].ToString(),
                            reader["都道府県カナ"].ToString(),
                            reader["市区町村カナ"].ToString(),
                            reader["町域カナ"].ToString(),
                            reader["都道府県"].ToString(),
                            reader["市区町村"].ToString(),
                            reader["町域"].ToString()
                        ));
                    }
                }
            }
            return results;
        }

        public List<AddressListItem> GetPrefectures()
        {
            var list = new List<AddressListItem>();
            string sql = @"
                SELECT MIN([全国地方公共団体コード]) AS Jis, [都道府県] AS Pref, MIN([都道府県カナ]) AS PrefKana
                FROM [T_PostalCode]
                GROUP BY [都道府県]
                ORDER BY MIN([全国地方公共団体コード])";

            using (var conn = new OleDbConnection(_connectionString))
            using (var cmd = new OleDbCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string pref = reader["Pref"].ToString();
                        string kana = reader["PrefKana"].ToString();
                        list.Add(new AddressListItem { DisplayText = pref, Kana = kana, KanaRowKey = KanaIndexHelper.GetRowKey(kana, pref) });
                    }
                }
            }
            return list;
        }

        public List<AddressListItem> GetCities(string prefecture)
        {
            var list = new List<AddressListItem>();
            string sql = @"
                SELECT [市区町村] AS City, MIN([市区町村カナ]) AS CityKana
                FROM [T_PostalCode]
                WHERE [都道府県] = @Pref
                GROUP BY [市区町村]
                ORDER BY MIN([市区町村カナ]), [市区町村]";

            using (var conn = new OleDbConnection(_connectionString))
            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Pref", prefecture);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string city = reader["City"].ToString();
                        string kana = reader["CityKana"].ToString();
                        list.Add(new AddressListItem { DisplayText = city, Kana = kana, KanaRowKey = KanaIndexHelper.GetRowKey(kana, city) });
                    }
                }
            }
            return list;
        }

        public List<AddressListItem> GetTowns(string prefecture, string city, bool candidatesOnly)
        {
            var list = new List<AddressListItem>();
            string filterSql = candidatesOnly ? "AND [町域] <> '以下に掲載がない場合'" : "";
            string sql = $@"
                SELECT [全国地方公共団体コード], [郵便番号], [都道府県カナ], [市区町村カナ], [町域カナ],
                       [都道府県], [市区町村], [町域]
                FROM [T_PostalCode]
                WHERE [都道府県] = @Pref AND [市区町村] = @City {filterSql}
                ORDER BY IIF([町域] = '以下に掲載がない場合', '0', '1'), [町域カナ], [町域]";

            using (var conn = new OleDbConnection(_connectionString))
            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Pref", prefecture);
                cmd.Parameters.AddWithValue("@City", city);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var addr = new PostalAddress(
                            reader["全国地方公共団体コード"].ToString(),
                            reader["郵便番号"].ToString(),
                            reader["都道府県カナ"].ToString(),
                            reader["市区町村カナ"].ToString(),
                            reader["町域カナ"].ToString(),
                            reader["都道府県"].ToString(),
                            reader["市区町村"].ToString(),
                            reader["町域"].ToString()
                        );
                        list.Add(new AddressListItem { DisplayText = addr.DisplayTown, Kana = addr.TownKana, KanaRowKey = KanaIndexHelper.GetRowKey(addr.TownKana, addr.DisplayTown), AddressData = addr });
                    }
                }
            }
            return list;
        }
    }
}