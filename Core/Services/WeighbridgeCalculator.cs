using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Weighbridge.Core.Services
{
    public class POQuotaCheckResult
    {
        public bool IsValid { get; set; }
        public bool IsExceeded { get; set; }
        public double GrossWeight { get; set; }
        public double RemainingQuota { get; set; }
        public double ExceedAmount { get; set; }
        public double ExceedPercentage { get; set; }
        public string Message { get; set; }
    }

    public class POLineItem
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public double OpenQty { get; set; }
    }

    public class SplitAllocationItem
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public double AllocatedQty { get; set; }
        public double RemainingOpenQty { get; set; }
    }

    public class SplitAllocationResult
    {
        public List<SplitAllocationItem> Allocations { get; set; } = new List<SplitAllocationItem>();
        public double TotalAllocated { get; set; }
        public double UnallocatedNetto { get; set; }
        public bool IsMultiLineSplit { get; set; }
        public bool IsFullyCovered { get; set; }
    }

    public static partial class WeighbridgeCalculator
    {
        public const double GRPOOverReceiptTolerancePercent = 1.0;

        public static string FormatKg(double qty)
        {
            if (qty % 1 == 0)
                return ((long)qty).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            else
                return qty.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string ParseWeight(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return "0";
            }

            rawData = rawData.Trim();
            var match = Regex.Match(rawData, @"[\d]+\.?[\d]*");
            if (match.Success)
            {
                if (double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double weight))
                {
                    return weight.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            return "0";
        }

        public static double CalculateNetto(double beratMasuk, double beratKeluar)
        {
            return Math.Round(Math.Abs(beratMasuk - beratKeluar), 2);
        }

        public static POQuotaCheckResult CheckPOQuota(double grossWeight, double remainingQuota, double tolerancePercent = 0.0)
        {
            var result = new POQuotaCheckResult
            {
                GrossWeight = grossWeight,
                RemainingQuota = remainingQuota,
                IsValid = true,
                IsExceeded = false,
                ExceedAmount = 0,
                ExceedPercentage = 0,
                Message = "Berat masih dalam kuota PO."
            };

            if (remainingQuota <= 0)
            {
                result.IsValid = false;
                result.IsExceeded = true;
                result.ExceedAmount = grossWeight;
                result.ExceedPercentage = 100.0;
                result.Message = "Sisa kuota PO sudah 0 (habis).";
                return result;
            }

            double maxAllowed = remainingQuota * (1.0 + (tolerancePercent / 100.0));

            if (grossWeight > maxAllowed)
            {
                result.IsValid = false;
                result.IsExceeded = true;
                result.ExceedAmount = Math.Round(grossWeight - remainingQuota, 2);
                result.ExceedPercentage = Math.Round((result.ExceedAmount / remainingQuota) * 100.0, 2);
                result.Message = $"Berat timbang ({FormatKg(grossWeight)} kg) melebihi sisa kuota PO ({FormatKg(remainingQuota)} kg) sebesar {FormatKg(result.ExceedAmount)} kg ({result.ExceedPercentage}%).";
            }

            return result;
        }

        public static SplitAllocationResult AllocateSplitContract(double totalNetto, List<POLineItem> availableLines)
        {
            var result = new SplitAllocationResult();
            if (totalNetto <= 0 || availableLines == null || availableLines.Count == 0)
            {
                result.UnallocatedNetto = totalNetto;
                result.IsFullyCovered = (totalNetto <= 0);
                return result;
            }

            double remainingToAllocate = totalNetto;

            for (int i = 0; i < availableLines.Count; i++)
            {
                var line = availableLines[i];
                if (line.OpenQty <= 0) continue;

                double alloc = Math.Min(remainingToAllocate, line.OpenQty);
                double remLine = line.OpenQty - alloc;

                result.Allocations.Add(new SplitAllocationItem
                {
                    LineNum = line.LineNum,
                    ItemCode = line.ItemCode,
                    AllocatedQty = alloc,
                    RemainingOpenQty = remLine
                });

                result.TotalAllocated += alloc;
                remainingToAllocate -= alloc;

                if (remainingToAllocate <= 0)
                {
                    break;
                }
            }

            result.UnallocatedNetto = Math.Round(remainingToAllocate, 2);
            result.IsMultiLineSplit = result.Allocations.Count > 1;
            result.IsFullyCovered = result.UnallocatedNetto <= 0;

            return result;
        }

        public static SplitContractWeightsResult CalculateSplitBySPB(double totalNettoSpb, double nettoSpbK1, double taraKebun)
        {
            var multi = CalculateSplitBySPB(totalNettoSpb, new[] { nettoSpbK1 }, taraKebun);
            var res = new SplitContractWeightsResult { NeedsSplit = multi.NeedsSplit, Error = multi.Error };
            if (!multi.NeedsSplit) return res;

            res.NettoK1 = multi.Netto[0];
            res.BrutoK1 = multi.Bruto[0];
            res.NettoK2 = multi.Netto[1];
            res.BrutoK2 = multi.Bruto[1];
            return res;
        }

        public static SplitMultiWeightsResult CalculateSplitBySPB(double totalNettoSpb, IList<double> nettoSpbTetap, double taraKebun)
        {
            var res = new SplitMultiWeightsResult();
            if (totalNettoSpb <= 0)
            {
                res.Error = "Netto SPB Kebun belum diisi.";
                return res;
            }

            int count = nettoSpbTetap?.Count ?? 0;
            if (count == 0)
            {
                res.Error = "Netto SPB K1 belum diisi. Isi sesuai SPB Kontrak 1 dari supplier.";
                return res;
            }

            double sum = 0;
            for (int i = 0; i < count; i++)
            {
                if (nettoSpbTetap[i] <= 0)
                {
                    res.Error = $"Netto SPB K{i + 1} belum diisi. Isi sesuai SPB Kontrak {i + 1} dari supplier.";
                    return res;
                }
                sum += Math.Round(nettoSpbTetap[i], 2);
            }

            if (sum >= totalNettoSpb)
            {
                res.Error = count == 1
                    ? $"Netto SPB K1 ({FormatKg(nettoSpbTetap[0])} kg) harus lebih kecil dari Netto SPB Kebun ({FormatKg(totalNettoSpb)} kg)."
                    : $"Netto SPB K1 + K{count} ({FormatKg(sum)} kg) harus lebih kecil dari Netto SPB Kebun ({FormatKg(totalNettoSpb)} kg), " +
                      $"sisanya untuk Kontrak K{count + 1}.";
                return res;
            }

            res.NeedsSplit = true;
            for (int i = 0; i < count; i++)
                res.Netto.Add(Math.Round(nettoSpbTetap[i], 2));
            res.Netto.Add(Math.Round(totalNettoSpb - sum, 2));
            foreach (double netto in res.Netto)
                res.Bruto.Add(Math.Round(netto + taraKebun, 2));
            return res;
        }

        public static K2QuotaValidationResult ValidateK2Quota(double nettoK2Needed, double sisaKuotaK2)
        {
            var res = new K2QuotaValidationResult
            {
                RequiredQty = Math.Round(nettoK2Needed, 2),
                AvailableQuota = Math.Round(sisaKuotaK2, 2)
            };

            if (nettoK2Needed <= 0)
            {
                res.IsValid = true;
                res.IsSufficient = true;
                res.RemainingQuotaAfterDeduction = res.AvailableQuota;
                res.Message = "Tidak ada alokasi ke Kontrak K2.";
                return res;
            }

            res.RemainingQuotaAfterDeduction = Math.Round(sisaKuotaK2 - nettoK2Needed, 2);

            if (sisaKuotaK2 >= nettoK2Needed)
            {
                res.IsValid = true;
                res.IsSufficient = true;
                res.Shortage = 0;
                res.Message = $"Kuota K2 mencukupi. Sisa kuota K2 setelah alokasi: {FormatKg(res.RemainingQuotaAfterDeduction)} kg.";
            }
            else
            {
                res.IsValid = false;
                res.IsSufficient = false;
                res.Shortage = Math.Round(nettoK2Needed - sisaKuotaK2, 2);
                res.Message = $"⚠ Kuota K2 ({FormatKg(sisaKuotaK2)} kg) TIDAK MENCUKUPI untuk menampung kekurangan K1 ({FormatKg(nettoK2Needed)} kg). Kurang: {FormatKg(res.Shortage)} kg.";
            }

            return res;
        }
    }

    public class SplitContractWeightsResult
    {
        public double NettoK1 { get; set; }
        public double BrutoK1 { get; set; }
        public double NettoK2 { get; set; }
        public double BrutoK2 { get; set; }
        public bool NeedsSplit { get; set; }
        public string Error { get; set; } = "";
    }

    public class SplitMultiWeightsResult
    {
        public List<double> Netto { get; } = new List<double>();
        public List<double> Bruto { get; } = new List<double>();
        public bool NeedsSplit { get; set; }
        public string Error { get; set; } = "";
    }

    public class SplitGRPOMultiResult
    {
        public List<double> NettoWB { get; } = new List<double>();
        public List<bool> LimitedBySAPQuota { get; } = new List<bool>();
        public List<double> LimpahanMasuk { get; } = new List<double>();
        public double TotalNettoWB { get; set; }
        public bool LastExceedsPlanned { get; set; }
        public double LastExceedAmount { get; set; }
    }

    public class K2QuotaValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsSufficient { get; set; }
        public double RequiredQty { get; set; }
        public double AvailableQuota { get; set; }
        public double RemainingQuotaAfterDeduction { get; set; }
        public double Shortage { get; set; }
        public string Message { get; set; }
    }

    public class SplitGRPONettoResult
    {
        public double NettoWB_K1 { get; set; }
        public double NettoWB_K2 { get; set; }
        public double TotalNettoWB { get; set; }
        public bool K2ExceedsPlanned { get; set; }
        public double K2ExceedAmount { get; set; }
        public bool ShouldCreateGRPO_K2 { get; set; }
        public double MinimumGRPO_K2_Kg { get; set; }
        public bool K1LimitedBySAPQuota { get; set; }
    }

    public static partial class WeighbridgeCalculator
    {
        public static SplitGRPONettoResult CalculateSplitGRPONetto(
            double totalNettoWB,
            double nettoKebunK1,
            double nettoKebunK2,
            double minimumGRPO_K2_Kg = 1.0,
            double sisaKuotaK1SAP = double.MaxValue)
        {
            var multi = CalculateSplitGRPONetto(totalNettoWB, new[] { nettoKebunK1, nettoKebunK2 }, new[] { sisaKuotaK1SAP });

            return new SplitGRPONettoResult
            {
                NettoWB_K1          = multi.NettoWB[0],
                NettoWB_K2          = multi.NettoWB[1],
                TotalNettoWB        = totalNettoWB,
                K2ExceedsPlanned    = multi.LastExceedsPlanned,
                K2ExceedAmount      = multi.LastExceedAmount,
                ShouldCreateGRPO_K2 = multi.NettoWB[1] >= minimumGRPO_K2_Kg,
                MinimumGRPO_K2_Kg   = minimumGRPO_K2_Kg,
                K1LimitedBySAPQuota = multi.LimitedBySAPQuota[0]
            };
        }
        
        public static SplitGRPOMultiResult CalculateSplitGRPONetto(double totalNettoWB, IList<double> nettoKebun, IList<double> sisaKuotaSAP)
        {
            var res = new SplitGRPOMultiResult { TotalNettoWB = totalNettoWB };
            int count = nettoKebun?.Count ?? 0;
            if (count == 0) return res;

            double remaining = Math.Max(0, totalNettoWB);
            double limpahan = 0;
            for (int i = 0; i < count - 1; i++)
            {
                double limit = sisaKuotaSAP != null && i < sisaKuotaSAP.Count ? sisaKuotaSAP[i] : double.MaxValue;
                bool sisanyaKontrakLanjutan = true;
                for (int j = i + 1; j < count; j++)
                    if (nettoKebun[j] > 0) { sisanyaKontrakLanjutan = false; break; }
                double wanted = sisanyaKontrakLanjutan ? remaining : Math.Min(remaining, nettoKebun[i] + limpahan);
                double alloc = Math.Min(wanted, Math.Max(0, limit));
                res.NettoWB.Add(alloc);
                res.LimitedBySAPQuota.Add(limit < wanted);
                res.LimpahanMasuk.Add(Math.Round(limpahan, 2));
                limpahan = wanted - alloc;
                remaining -= alloc;
            }

            double last = Math.Max(0, Math.Round(remaining, 2));
            res.NettoWB.Add(last);
            res.LimitedBySAPQuota.Add(false);
            res.LimpahanMasuk.Add(Math.Round(limpahan, 2));

            double plannedLast = nettoKebun[count - 1];
            res.LastExceedsPlanned = plannedLast > 0 && last > plannedLast + 0.001;
            res.LastExceedAmount = res.LastExceedsPlanned ? Math.Round(last - plannedLast, 2) : 0;
            return res;
        }

        public static double SumPendingNetto(IEnumerable<Weighbridge.Core.Models.PendingTicketInfo> pendingTickets)
        {
            if (pendingTickets == null) return 0;

            double total = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pt in pendingTickets)
            {
                if (pt == null) continue;
                if (string.IsNullOrEmpty(pt.TiketNum) || seen.Add(pt.TiketNum))
                {
                    total += Math.Max(0, pt.Netto);
                }
            }
            return total;
        }
        public static double EstimatePendingNetto(double brutoKebun, double taraKebun, double nettoKebun, double beratMasuk)
        {
            if (nettoKebun > 0) return nettoKebun;
            if (brutoKebun > 0 && taraKebun > 0) return Math.Max(0, brutoKebun - taraKebun);
            if (beratMasuk > 0 && taraKebun > 0) return Math.Max(0, beratMasuk - taraKebun);
            return 0;
        }

        public static bool IsNettoKebunMissing(string docType, double nettoKebun)
        {
            return string.Equals(docType, "PO", StringComparison.OrdinalIgnoreCase) && nettoKebun <= 0;
        }

        public static double CalculateEffectiveQuota(double sisaKuotaSAP, double pendingNetto)
        {
            return Math.Max(0, sisaKuotaSAP - Math.Max(0, pendingNetto));
        }
        public static bool IsTaraNettoKebunSwapped(double taraKebun, double nettoKebun, double taraWB)
        {
            if (taraKebun <= 0 || nettoKebun <= 0 || taraWB <= 0) return false;
            if (taraKebun <= nettoKebun) return false;
            return Math.Abs(nettoKebun - taraWB) < Math.Abs(taraKebun - taraWB);
        }

        public static bool RequiresSplitKontrak(double nettoKebun, double sisaKuotaEfektif)
        {
            return nettoKebun > 0 && nettoKebun > sisaKuotaEfektif + 0.001;
        }
    }
}

