using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Weighbridge.Core.Helpers
{
    public static class ReportDataHelper
    {
        private const string ColTiket = "Nomor Tiket";
        private const string ColMasuk = "Berat Masuk";
        private const string ColKeluar = "Berat Keluar";
        private const string ColNetto = "Berat Netto";

        public static void ApplySplitBrutoPerKontrak(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains(ColTiket) || !dt.Columns.Contains(ColMasuk) ||
                !dt.Columns.Contains(ColKeluar) || !dt.Columns.Contains(ColNetto))
                return;

            var rowsPerTiket = new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in dt.Rows)
            {
                string tiket = row[ColTiket] == DBNull.Value ? "" : row[ColTiket].ToString().Trim();
                if (tiket.Length == 0) continue;
                if (!rowsPerTiket.TryGetValue(tiket, out var list))
                    rowsPerTiket[tiket] = list = new List<DataRow>();
                list.Add(row);
            }

            DataColumn colMasuk = dt.Columns[ColMasuk];
            foreach (var group in rowsPerTiket.Values)
            {
                if (group.Count < 2) continue;
                foreach (DataRow row in group)
                {
                    double netto = ParseKg(row[ColNetto]);
                    double tara = ParseKg(row[ColKeluar]);
                    if (netto <= 0 || tara <= 0) continue;

                    colMasuk.ReadOnly = false;
                    double bruto = Math.Round(netto + tara, 2);
                    row[colMasuk] = colMasuk.DataType == typeof(string)
                        ? (object)(bruto.ToString("0.##", CultureInfo.InvariantCulture) + " kg")
                        : Convert.ChangeType(bruto, colMasuk.DataType, CultureInfo.InvariantCulture);
                }
            }
        }

        public static double ParseKg(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            if (value is double d) return d;
            if (value is decimal m) return (double)m;
            var match = Regex.Match(value.ToString(), @"-?\d+(\.\d+)?");
            return match.Success && double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
        }
    }
}
