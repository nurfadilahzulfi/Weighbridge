using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Weighbridge.Core.Helpers
{
    public static class SplitTicketRowHelper
    {
        public static void GetK1K2(DataTable dt, out DataRow k1, out DataRow k2)
        {
            if (dt == null || dt.Rows.Count < 2)
                throw new ArgumentException("Tiket split kontrak harus memiliki minimal 2 record.", nameof(dt));

            List<DataRow> rows = GetOrderedRows(dt);
            k1 = rows[0];
            k2 = rows[1];
        }

        public static List<DataRow> GetOrderedRows(DataTable dt)
        {
            var rows = new List<DataRow>();
            if (dt == null) return rows;
            foreach (DataRow row in dt.Rows) rows.Add(row);

            string codeCol = dt.Columns.Contains("Code") ? "Code" : (dt.Columns.Contains("CODE") ? "CODE" : null);
            if (codeCol == null) return rows;

            // Insertion sort: stabil untuk record yang Code-nya kosong / sama
            for (int i = 1; i < rows.Count; i++)
            {
                DataRow current = rows[i];
                int j = i - 1;
                while (j >= 0 && CompareRowCode(rows[j], current, codeCol) > 0)
                {
                    rows[j + 1] = rows[j];
                    j--;
                }
                rows[j + 1] = current;
            }
            return rows;
        }

        private static int CompareRowCode(DataRow a, DataRow b, string codeCol)
        {
            string codeA = a[codeCol]?.ToString()?.Trim() ?? "";
            string codeB = b[codeCol]?.ToString()?.Trim() ?? "";
            if (codeA.Length == 0 || codeB.Length == 0) return 0;
            return CompareCode(codeA, codeB);
        }

        public static (double Bruto, double Tara, double Netto) CombineKebun(double nettoK1, double nettoK2, double taraKebun)
        {
            return CombineKebun(new[] { nettoK1, nettoK2 }, taraKebun);
        }

        public static (double Bruto, double Tara, double Netto) CombineKebun(IEnumerable<double> nettoPerKontrak, double taraKebun)
        {
            double netto = 0;
            foreach (double n in nettoPerKontrak) netto += n;
            netto = Math.Round(netto, 2);
            double tara = Math.Round(taraKebun, 2);
            return (Math.Round(netto + tara, 2), tara, netto);
        }

        public static string BuildSplitNote(IList<string> refs, IList<double> nettos)
        {
            var parts = new List<string>();
            for (int i = 0; i < refs.Count && i < nettos.Count; i++)
                parts.Add($"{refs[i]}: {nettos[i]:N0} KG");
            return string.Join(" SPLIT ", parts);
        }

        public static int CompareCode(string a, string b)
        {
            var ma = Regex.Match(a, @"^(.*?)(\d+)$");
            var mb = Regex.Match(b, @"^(.*?)(\d+)$");
            if (ma.Success && mb.Success && string.Equals(ma.Groups[1].Value, mb.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
            {
                string da = ma.Groups[2].Value.TrimStart('0');
                string db = mb.Groups[2].Value.TrimStart('0');
                if (da.Length != db.Length) return da.Length.CompareTo(db.Length);
                return string.CompareOrdinal(da, db);
            }
            return string.CompareOrdinal(a, b);
        }
    }
}
