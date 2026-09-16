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

    public static class WeighbridgeCalculator
    {
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
                if (double.TryParse(match.Value, out double weight))
                {
                    return weight.ToString("0.##");
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
                result.IsExceeded = true;
                result.ExceedAmount = Math.Round(grossWeight - remainingQuota, 2);
                result.ExceedPercentage = Math.Round((result.ExceedAmount / remainingQuota) * 100.0, 2);
                result.Message = $"Berat timbang ({grossWeight:N0} Kg) melebihi sisa kuota PO ({remainingQuota:N0} Kg) sebesar {result.ExceedAmount:N0} Kg ({result.ExceedPercentage}%).";
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
    }
}
