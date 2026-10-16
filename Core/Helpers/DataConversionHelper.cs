using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Weighbridge.Core.Helpers
{
    public static class DataConversionHelper
    {
        public static double SafeToDouble(object dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value) return 0;
            try
            {
                if (dbValue is Sap.Data.Hana.HanaDecimal hd) return (double)hd.ToDecimal();
                if (dbValue is decimal d) return (double)d;
                if (dbValue is double dbl) return dbl;
                if (dbValue is float f) return f;
                if (dbValue is int i) return i;
                if (dbValue is long l) return l;

                string s = dbValue.ToString().Trim();
                if (s.Contains(",") && !s.Contains("."))
                {
                    s = s.Replace(",", ".");
                }
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                    return result;

                return Convert.ToDouble(dbValue, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        // "12,000" / "12.000" / "1,250,500": pemisah ribuan (hasil FormatKg atau ketikan operator), bukan desimal.
        private static readonly Regex ThousandsGroupedWeight = new Regex(@"^-?[1-9]\d{0,2}([.,])\d{3}(\1\d{3})*$");

        public static double ParseWeightInput(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            string s = text.Trim().Replace(" ", "");
            int lastComma = s.LastIndexOf(',');
            int lastDot = s.LastIndexOf('.');

            if (lastComma >= 0 && lastDot >= 0)
            {
                // Dua jenis pemisah: yang terakhir adalah desimal ("12,500.50" atau "12.500,50")
                char dec = lastComma > lastDot ? ',' : '.';
                char grp = dec == ',' ? '.' : ',';
                s = s.Replace(grp.ToString(), "").Replace(dec, '.');
            }
            else if (ThousandsGroupedWeight.IsMatch(s))
            {
                s = s.Replace(",", "").Replace(".", "");
            }
            else if (lastComma >= 0)
            {
                s = s.Replace(",", ".");
            }

            if (double.TryParse(s, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double val))
                return val;
            return 0;
        }

        public static string ParseWeight(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
                return "0";

            rawData = rawData.Trim();
            var match = Regex.Match(rawData, @"[\d]+\.?[\d]*");
            if (match.Success)
            {
                if (double.TryParse(match.Value, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double weight))
                {
                    return weight.ToString("0.##", CultureInfo.InvariantCulture);
                }
            }

            return "0";
        }

        public static string GetColumnValue(System.Data.IDataReader reader, params string[] columnNames)
        {
            for (int i = 0; i < columnNames.Length; i++)
            {
                string col = columnNames[i];
                for (int c = 0; c < reader.FieldCount; c++)
                {
                    if (string.Equals(reader.GetName(c), col, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!reader.IsDBNull(c))
                        {
                            string val = reader.GetValue(c)?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(val)) return val;
                        }
                    }
                }
            }
            return string.Empty;
        }

        public static DateTime? GetColumnDate(System.Data.IDataReader reader, params string[] columnNames)
        {
            for (int i = 0; i < columnNames.Length; i++)
            {
                string col = columnNames[i];
                for (int c = 0; c < reader.FieldCount; c++)
                {
                    if (string.Equals(reader.GetName(c), col, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!reader.IsDBNull(c))
                        {
                            if (DateTime.TryParse(reader.GetValue(c)?.ToString(), out DateTime dtVal))
                            {
                                return dtVal;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static string PreferVendorRef(string vendorRef, string docNum)
        {
            return !string.IsNullOrWhiteSpace(vendorRef) ? vendorRef.Trim() : (docNum ?? "").Trim();
        }

        public static int SafeToInt(object dbValue, int defaultValue = 0)
        {
            if (dbValue == null || dbValue == DBNull.Value) return defaultValue;
            try
            {
                if (dbValue is int i) return i;
                if (dbValue is long l) return (int)l;
                if (dbValue is short s) return s;
                if (dbValue is byte b) return b;
                if (dbValue is decimal d) return (int)d;
                if (dbValue is double dbl) return (int)dbl;
                if (int.TryParse(dbValue.ToString().Trim(), out int res)) return res;
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        // SP TANIMASLIVE mengirim kontrak kosong sebagai "0"
        public static string NormalizeKontrak(object value)
        {
            string s = value == null || value == DBNull.Value ? "" : value.ToString().Trim();
            return s == "0" ? "" : s;
        }

        public static DateTime? ParseWaktuMasuk(object value, DateTime? tanggal)
        {
            if (value == null || value == DBNull.Value) return null;
            DateTime baseDate = (tanggal ?? DateTime.Today).Date;
            if (value is DateTime dt) return baseDate.Add(dt.TimeOfDay);
            if (value is TimeSpan ts) return baseDate.Add(ts);

            string s = value.ToString().Trim();
            if (s.Contains(":") && TimeSpan.TryParse(s, out TimeSpan parsed)) return baseDate.Add(parsed);
            if (int.TryParse(s, out int hhmm) && hhmm >= 0 && hhmm <= 2359 && hhmm % 100 < 60)
                return baseDate.AddHours(hhmm / 100).AddMinutes(hhmm % 100);
            return null;
        }
    }
}
