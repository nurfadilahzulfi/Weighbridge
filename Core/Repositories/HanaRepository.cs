using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.Data;
using Weighbridge.Connection;
using Weighbridge.Core.Config;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;

namespace Weighbridge.Core.Repositories
{
    public class HanaRepository : IHanaRepository
    {
        private readonly HANAConnetion _hanaConn;

        public HanaRepository()
        {
            _hanaConn = new HANAConnetion();
        }
        private string GetConnectionString()
        {
            return _hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);
        }

        public string GenerateTiketNumber()
        {
            string tiketNum = "";
            string wbCategory = ConfigManager.wbConfig.prefix.ToString();

            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = "CALL SOL_SP_ADDON_WB_GENERATE_TIKET_NUMBER('" + wbCategory + "')";
                using (var command = new HanaCommand(queryString, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tiketNum = reader["TIKETNUM"].ToString();
                    }
                }
            }
            return tiketNum;
        }

        public string GetNextWBCode()
        {
            string code = "";
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = "CALL SOL_SP_ADDON_WB_MAX_UDO_CODE()";
                using (var command = new HanaCommand(queryString, connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows && reader.Read())
                        code = reader["WBCODE"].ToString();
                }
            }
            return code;
        }

        public List<string> GetKodeDokumenPabean()
        {
            var result = new List<string>();
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = "CALL SOL_SP_ADDON_WB_GET_KODE_DOKUMEN()";
                using (var command = new HanaCommand(queryString, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(reader["KODEDOKUMEN"].ToString());
                    }
                }
            }
            return result;
        }

        public DataTable GetTransporterInternal()
        {
            string query = "SELECT \"CardCode\", \"CardName\" FROM \"OCRD\" WHERE \"GroupCode\" = 103";
            var dt = new DataTable();

            using (var conn = new HanaConnection(GetConnectionString()))
            using (var cmd = new HanaCommand(query, conn))
            {
                var da = new HanaDataAdapter(cmd);
                conn.Open();
                da.Fill(dt);
            }
            return dt;
        }

        public DataTable SearchBaseDocument(string docType, string vendorRef)
        {
            var dt = new DataTable();
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = $"CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC('{docType}','{vendorRef}')";
                using (var command = new HanaCommand(queryString, connection))
                {
                    var da = new HanaDataAdapter(command);
                    da.Fill(dt);
                }
            }
            return dt;
        }

        public DocQuotaInfo GetDocQuotaInfo(string docType, int docEntry, int lineNum)
        {
            string tableHeader = docType == "PO" ? "OPOR" : "ORDR";
            string tableLine = docType == "PO" ? "POR1" : "RDR1";
            var result = new DocQuotaInfo();

            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string quotaQuery = $"SELECT T0.\"DocStatus\" AS HD_STATUS, T1.\"LineStatus\" AS LN_STATUS, T1.\"OpenQty\" AS OPEN_QTY, T1.\"Quantity\" AS TOTAL_QTY " +
                                    $"FROM \"{tableHeader}\" T0 INNER JOIN \"{tableLine}\" T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                                    $"WHERE T0.\"DocEntry\" = {docEntry} AND T1.\"LineNum\" = {lineNum}";

                using (var cmd = new HanaCommand(quotaQuery, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.HeaderStatus = reader["HD_STATUS"].ToString();
                        result.LineStatus = reader["LN_STATUS"].ToString();
                        result.OpenQty = DataConversionHelper.SafeToDouble(reader["OPEN_QTY"]);
                        result.TotalQty = DataConversionHelper.SafeToDouble(reader["TOTAL_QTY"]);
                    }
                }
            }
            return result;
        }

        public (double openQty, double totalQty) GetFreshQuota(string docType, int docEntry, int lineNum)
        {
            string tableLine = docType == "PO" ? "POR1" : "RDR1";
            double openQty = 0, totalQty = 0;

            try
            {
                using (var conn = new HanaConnection(GetConnectionString()))
                {
                    conn.Open();
                    string freshQ = $"SELECT T1.\"OpenQty\", T1.\"Quantity\" FROM \"{tableLine}\" T1 WHERE T1.\"DocEntry\" = {docEntry} AND T1.\"LineNum\" = {lineNum}";
                    using (var cmd = new HanaCommand(freshQ, conn))
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            if (rdr["OpenQty"] != DBNull.Value) openQty = DataConversionHelper.SafeToDouble(rdr["OpenQty"]);
                            if (rdr["Quantity"] != DBNull.Value) totalQty = DataConversionHelper.SafeToDouble(rdr["Quantity"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetFreshQuota error: " + ex.Message);
            }

            return (openQty, totalQty);
        }

        public string GetFreshKontrakNo(string docType, int docEntry)
        {
            string tableHeader = docType == "PO" ? "OPOR" : "ORDR";

            try
            {
                using (var conn = new HanaConnection(GetConnectionString()))
                {
                    conn.Open();
                    string kontrakQ = $"SELECT COALESCE(\"U_SOL_NO_KONTRAK\", '') AS \"KONTRAK\" FROM \"{tableHeader}\" WHERE \"DocEntry\" = {docEntry}";
                    using (var cmd = new HanaCommand(kontrakQ, conn))
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            return DataConversionHelper.NormalizeKontrak(rdr["KONTRAK"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetFreshKontrakNo error: " + ex.Message);
            }

            return "";
        }

        public PabeanData GetPabeanData(string docType, int docEntry)
        {
            string tableHeader = docType == "PO" ? "OPOR" : "ORDR";
            var result = new PabeanData();

            try
            {
                using (var connection = new HanaConnection(GetConnectionString()))
                {
                    connection.Open();
                    string pabeanQuery = $"SELECT * FROM \"{tableHeader}\" WHERE \"DocEntry\" = {docEntry}";
                    using (var cmdPabean = new HanaCommand(pabeanQuery, connection))
                    using (var rdr = cmdPabean.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            result.NomorSPB = DataConversionHelper.GetColumnValue(rdr, "U_SOL_NOMOR_SPB", "U_SOL_NO_SPB", "SOL_NOMOR_SPB", "SOL_NO_SPB");
                            result.TanggalSPB = DataConversionHelper.GetColumnDate(rdr, "U_SOL_TANGGAL_SPB", "SOL_TANGGAL_SPB", "U_SOL_TGL_SPB");
                            result.StatusJalur = DataConversionHelper.GetColumnValue(rdr, "U_SOL_STATUS_JALUR", "SOL_STATUS_JALUR", "U_STATUS_JALUR");
                            result.FasilitasPabean = DataConversionHelper.GetColumnValue(rdr, "U_SOL_FASILITAS_PABEAN", "SOL_FASILITAS_PABEAN", "U_SOL_FASILITAS");
                            result.KodeDokumen = DataConversionHelper.GetColumnValue(rdr, "U_SOL_KODE_DOKUMEN", "SOL_KODE_DOKUMEN");
                            result.NomorDokumen = DataConversionHelper.GetColumnValue(rdr, "U_SOL_NOMOR_DOKUMEN", "U_SOL_NO_DOKUMEN", "SOL_NOMOR_DOKUMEN");
                            result.TanggalDokumen = DataConversionHelper.GetColumnDate(rdr, "U_SOL_TANGGAL_DOKUMEN", "SOL_TANGGAL_DOKUMEN", "U_SOL_TGL_DOKUMEN");
                            result.NomorAju = DataConversionHelper.GetColumnValue(rdr, "U_SOL_NOMOR_AJU", "U_SOL_NO_AJU", "SOL_NOMOR_AJU");
                            result.NoKontrak = DataConversionHelper.NormalizeKontrak(DataConversionHelper.GetColumnValue(rdr, "U_SOL_NO_KONTRAK", "U_SOL_KONTRAK", "KONTRAK"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetPabeanData error: " + ex.Message);
            }

            return result;
        }

        public List<DocumentLookupItem> GetOpenDocuments(string docType)
        {
            string tableHeader = docType == "PO" ? "OPOR" : "ORDR";
            string tableLine = docType == "PO" ? "POR1" : "RDR1";
            string contractCol = docType == "PO" ? "COALESCE(T0.\"U_SOL_NO_KONTRAK\", '') AS \"ContractNo\", " : "'' AS \"ContractNo\", ";
            var freshList = new List<DocumentLookupItem>();

            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string query = $"SELECT T0.\"DocEntry\", T0.\"DocNum\", T0.\"CardCode\", T0.\"CardName\", " +
                               $"COALESCE(T0.\"NumAtCard\", '') AS \"NumAtCard\", " +
                               contractCol +
                               $"T1.\"LineNum\", T1.\"ItemCode\", T1.\"Dscription\", T1.\"OpenQty\", T1.\"Quantity\", " +
                               $"T0.\"DocStatus\", T1.\"LineStatus\" " +
                               $"FROM \"{tableHeader}\" T0 " +
                               $"INNER JOIN \"{tableLine}\" T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                               $"WHERE T0.\"DocStatus\" = 'O' AND T1.\"LineStatus\" = 'O' " +
                               $"ORDER BY T0.\"DocNum\" DESC";

                using (var cmd = new HanaCommand(query, connection))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var item = new DocumentLookupItem
                        {
                            DocEntry = DataConversionHelper.SafeToInt(rdr["DocEntry"]),
                            DocNum = rdr["DocNum"]?.ToString() ?? "",
                            CardCode = rdr["CardCode"]?.ToString() ?? "",
                            CardName = rdr["CardName"]?.ToString() ?? "",
                            NumAtCard = rdr["NumAtCard"]?.ToString() ?? "",
                            ContractNo = rdr["ContractNo"]?.ToString() ?? "",
                            LineNum = DataConversionHelper.SafeToInt(rdr["LineNum"]),
                            ItemCode = rdr["ItemCode"]?.ToString() ?? "",
                            ItemName = rdr["Dscription"]?.ToString() ?? "",
                            OpenQty = DataConversionHelper.SafeToDouble(rdr["OpenQty"]),
                            Quantity = DataConversionHelper.SafeToDouble(rdr["Quantity"]),
                            DocStatus = rdr["DocStatus"]?.ToString() ?? "",
                            LineStatus = rdr["LineStatus"]?.ToString() ?? ""
                        };
                        freshList.Add(item);
                    }
                }
            }
            return freshList;
        }

        public List<PendingTicketInfo> GetPendingTickets(string docType, string searchKey)
        {
            var list = new List<PendingTicketInfo>();
            if (string.IsNullOrWhiteSpace(searchKey)) return list;

            var seenTickets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var connection = new HanaConnection(GetConnectionString()))
                {
                    connection.Open();
                    string safeKey = searchKey.Trim().Replace("'", "''");

                    // 1. Cek via Stored Procedure SP_ADDON_WB_SEARCH_BASE_DOC_JOIN_WB_DATA
                    try
                    {
                        string queryString = $"CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC_JOIN_WB_DATA('{docType}','{safeKey}')";
                        using (var cmd = new HanaCommand(queryString, connection))
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                string status = "";
                                string weighout = "";
                                for (int i = 0; i < rdr.FieldCount; i++)
                                {
                                    string col = rdr.GetName(i);
                                    if (string.Equals(col, "U_SOL_STATUS", StringComparison.OrdinalIgnoreCase))
                                        status = rdr.GetValue(i)?.ToString()?.Trim() ?? "";
                                    else if (string.Equals(col, "U_SOL_WEIGHOUT", StringComparison.OrdinalIgnoreCase))
                                        weighout = rdr.GetValue(i)?.ToString()?.Trim() ?? "";
                                }

                                bool isPending = status.Equals("WEIGHIN", StringComparison.OrdinalIgnoreCase) ||
                                                 status.Equals("I", StringComparison.OrdinalIgnoreCase) ||
                                                 (!string.IsNullOrEmpty(status) && !status.Equals("SYNCSAP", StringComparison.OrdinalIgnoreCase) && !status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase) && !status.Equals("CANCEL", StringComparison.OrdinalIgnoreCase)) ||
                                                 (!string.IsNullOrEmpty(weighout) && !weighout.Equals("Y", StringComparison.OrdinalIgnoreCase));

                                if (isPending)
                                {
                                    string tkt = "", nopol = "", supir = "";
                                    double bruto = 0, netto = 0, tara = 0, masuk = 0;

                                    for (int i = 0; i < rdr.FieldCount; i++)
                                    {
                                        string col = rdr.GetName(i);
                                        if (string.Equals(col, "U_SOL_TIKET_WB", StringComparison.OrdinalIgnoreCase)) tkt = rdr.GetValue(i)?.ToString()?.Trim() ?? "";
                                        else if (string.Equals(col, "U_SOL_NOPOL", StringComparison.OrdinalIgnoreCase)) nopol = rdr.GetValue(i)?.ToString() ?? "";
                                        else if (string.Equals(col, "U_SOL_NAMA_SUPIR", StringComparison.OrdinalIgnoreCase)) supir = rdr.GetValue(i)?.ToString() ?? "";
                                        else if (string.Equals(col, "U_SOL_BRUTO_KEBUN", StringComparison.OrdinalIgnoreCase))
                                        {
                                            bruto = Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(rdr.GetValue(i));
                                        }
                                        else if (string.Equals(col, "U_SOL_NETTO_KEBUN", StringComparison.OrdinalIgnoreCase))
                                        {
                                            netto = Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(rdr.GetValue(i));
                                        }
                                        else if (string.Equals(col, "U_SOL_TARA_KEBUN", StringComparison.OrdinalIgnoreCase))
                                        {
                                            tara = Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(rdr.GetValue(i));
                                        }
                                        else if (string.Equals(col, "U_SOL_BERAT_MASUK", StringComparison.OrdinalIgnoreCase))
                                        {
                                            masuk = Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(rdr.GetValue(i));
                                        }
                                    }

                                    netto = Weighbridge.Core.Services.WeighbridgeCalculator.EstimatePendingNetto(bruto, tara, netto, masuk);

                                    if (!string.IsNullOrEmpty(tkt) && seenTickets.Add(tkt))
                                    {
                                        list.Add(new PendingTicketInfo
                                        {
                                            TiketNum = tkt,
                                            Nopol = nopol,
                                            NamaSupir = supir,
                                            Status = "BELUM GRPO / BELUM TIMBANG KELUAR",
                                            Bruto = bruto,
                                            Netto = netto
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception exSP)
                    {
                        System.Diagnostics.Debug.WriteLine("GetPendingTickets SP error: " + exSP.Message);
                    }

                    // 2. Cek langsung ke tabel UDO @SOL_WB_DATA untuk memastikan seluruh tiket WEIGHIN terdata akurat
                    try
                    {
                        string udoQuery = $@"SELECT 
                                                T0.""U_SOL_TIKET_WB"",
                                                COALESCE(T0.""U_SOL_NOPOL"", '') AS ""U_SOL_NOPOL"",
                                                COALESCE(T0.""U_SOL_NAMA_SUPIR"", '') AS ""U_SOL_NAMA_SUPIR"",
                                                COALESCE(T0.""U_SOL_BRUTO_KEBUN"", '0') AS ""U_SOL_BRUTO_KEBUN"",
                                                COALESCE(T0.""U_SOL_NETTO_KEBUN"", '0') AS ""U_SOL_NETTO_KEBUN"",
                                                COALESCE(T0.""U_SOL_TARA_KEBUN"", '0') AS ""U_SOL_TARA_KEBUN"",
                                                COALESCE(T0.""U_SOL_BERAT_MASUK"", 0) AS ""U_SOL_BERAT_MASUK""
                                            FROM ""@SOL_WB_DATA"" T0
                                            WHERE (T0.""U_SOL_DOCNUM_SAP"" = '{safeKey}' OR T0.""U_SOL_NO_KONTRAK"" = '{safeKey}' OR T0.""U_SOL_VENDOR_REF"" = '{safeKey}')
                                              AND (T0.""U_SOL_STATUS"" = 'WEIGHIN' OR T0.""U_SOL_STATUS"" = 'I')";

                        using (var udoCmd = new HanaCommand(udoQuery, connection))
                        using (var udoRdr = udoCmd.ExecuteReader())
                        {
                            while (udoRdr.Read())
                            {
                                string tkt = udoRdr["U_SOL_TIKET_WB"]?.ToString()?.Trim() ?? "";
                                if (string.IsNullOrEmpty(tkt)) continue;

                                double bruto = 0, netto = 0, tara = 0, masuk = 0;
                                bruto = Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(udoRdr["U_SOL_BRUTO_KEBUN"]);
                                netto = Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(udoRdr["U_SOL_NETTO_KEBUN"]);
                                tara = Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(udoRdr["U_SOL_TARA_KEBUN"]);
                                masuk = Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(udoRdr["U_SOL_BERAT_MASUK"]);

                                netto = Weighbridge.Core.Services.WeighbridgeCalculator.EstimatePendingNetto(bruto, tara, netto, masuk);

                                if (seenTickets.Add(tkt))
                                {
                                    list.Add(new PendingTicketInfo
                                    {
                                        TiketNum = tkt,
                                        Nopol = udoRdr["U_SOL_NOPOL"]?.ToString() ?? "",
                                        NamaSupir = udoRdr["U_SOL_NAMA_SUPIR"]?.ToString() ?? "",
                                        Status = "BELUM GRPO / BELUM TIMBANG KELUAR",
                                        Bruto = bruto,
                                        Netto = netto
                                    });
                                }
                                else
                                {
                                    var existing = list.Find(p => string.Equals(p.TiketNum, tkt, StringComparison.OrdinalIgnoreCase));
                                    if (existing != null && existing.Netto <= 0 && netto > 0)
                                    {
                                        existing.Netto = netto;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception exUDO)
                    {
                        System.Diagnostics.Debug.WriteLine("GetPendingTickets UDO error: " + exUDO.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetPendingTickets error: " + ex.Message);
            }

            return list;
        }

        public string GetWBCodeByTiket(string tiketNum)
        {
            string code = "";
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_CODE_UDO('" + tiketNum + "')";
                using (var command = new HanaCommand(queryString, connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            code = reader["CODE"].ToString();
                        }
                    }
                }
            }
            return code;
        }

        // Semua Code UDO untuk satu tiket (tiket split punya record K1 dan K2)
        public List<string> GetWBCodesByTiket(string tiketNum)
        {
            var codes = new List<string>();
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_CODE_UDO('" + (tiketNum ?? "").Replace("'", "''") + "')";
                using (var command = new HanaCommand(queryString, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string code = reader["CODE"]?.ToString()?.Trim() ?? "";
                        if (code != "" && !codes.Contains(code)) codes.Add(code);
                    }
                }
            }
            return codes;
        }

        public (string isIns, int baseType, int docEntry, int lineNum) SearchPOSOReserve(string docType, int docEntrySO)
        {
            string isIns = "";
            int baseType = 0, docEntry = 0, lineNum = 0;

            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = $"CALL SOL_SP_ADDON_WB_SEARCH_PO_SO_RESERVE('{docType}', {docEntrySO})";
                using (var command = new HanaCommand(queryString, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        isIns = reader["ISINS"]?.ToString() ?? "";
                        baseType = DataConversionHelper.SafeToInt(reader["BASETYPE"]);
                        docEntry = DataConversionHelper.SafeToInt(reader["DOCENTRY"]);
                        lineNum = DataConversionHelper.SafeToInt(reader["LINENUM"]);
                    }
                }
            }

            return (isIns, baseType, docEntry, lineNum);
        }

        public string GetPostedDocNumsForTicket(string tableName, string tiketNum)
        {
            if (string.IsNullOrWhiteSpace(tiketNum)) return "";
            var docNums = new List<string>();
            try
            {
                using (var connection = new HanaConnection(GetConnectionString()))
                {
                    connection.Open();
                    string safeTiket = tiketNum.Trim().Replace("'", "''");
                    string query = $"SELECT \"DocNum\" FROM \"{tableName}\" WHERE \"U_SOL_NO_TIKET_WB\" = '{safeTiket}' AND \"CANCELED\" = 'N'";
                    using (var cmd = new HanaCommand(query, connection))
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                            docNums.Add(rdr["DocNum"]?.ToString() ?? "");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPostedDocNumsForTicket({tableName},{tiketNum}) error: " + ex.Message);
            }
            return string.Join(", ", docNums);
        }

        public string GetDocNumByEntry(string tableName, string docEntry)
        {
            string docNum = "";
            try
            {
                using (var connection = new HanaConnection(GetConnectionString()))
                {
                    connection.Open();
                    string query = $"SELECT \"DocNum\" FROM \"{tableName}\" WHERE \"DocEntry\" = {docEntry}";
                    using (var cmd = new HanaCommand(query, connection))
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                            docNum = rdr["DocNum"]?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDocNumByEntry({tableName},{docEntry}) error: " + ex.Message);
            }
            return docNum;
        }

        public List<string> GetNomorTangki(string itemName)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(itemName)) return list;

            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string resolvedName = itemName.Trim();
                try
                {
                    string safeItem = resolvedName.Replace("'", "''");
                    string resolveQuery = $"SELECT \"ItemName\" FROM \"OITM\" WHERE \"ItemCode\" = '{safeItem}'";
                    using (var cmdResolve = new HanaCommand(resolveQuery, connection))
                    {
                        var obj = cmdResolve.ExecuteScalar();
                        if (obj != null && obj != DBNull.Value && !string.IsNullOrWhiteSpace(obj.ToString()))
                        {
                            resolvedName = obj.ToString().Trim();
                        }
                    }
                }
                catch { }

                string safeResolved = resolvedName.Replace("'", "''");
                string queryString = $"CALL SOL_SP_ADDON_WB_GET_NOMOR_TANGKI('{safeResolved}')";
                using (var command = new HanaCommand(queryString, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(reader["NOMORTANGKI"].ToString());
                    }
                }

                if (list.Count == 0 && !string.Equals(resolvedName, itemName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    string safeOriginal = itemName.Trim().Replace("'", "''");
                    string fallbackQuery = $"CALL SOL_SP_ADDON_WB_GET_NOMOR_TANGKI('{safeOriginal}')";
                    using (var cmdFallback = new HanaCommand(fallbackQuery, connection))
                    using (var readerFallback = cmdFallback.ExecuteReader())
                    {
                        while (readerFallback.Read())
                        {
                            list.Add(readerFallback["NOMORTANGKI"].ToString());
                        }
                    }
                }
            }
            return list;
        }

        public DataTable SearchBaseDocJoinWbData(string docType, string docNum)
        {
            var dt = new DataTable();
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = $"CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC_JOIN_WB_DATA('{docType}','{docNum}')";
                using (var command = new HanaCommand(queryString, connection))
                using (var adapter = new HanaDataAdapter(command))
                {
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        public DataTable SearchTicketByTiketNum(string noTiket)
        {
            var dt = new DataTable();
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = $"CALL SOL_SP_ADDON_WB_SEARCH_NO_TIKET('{noTiket.Trim()}')";
                using (var command = new HanaCommand(queryString, connection))
                using (var adapter = new HanaDataAdapter(command))
                {
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        public List<TicketLookupItem> GetPendingTicketLookupList()
        {
            var list = new List<TicketLookupItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string query = @"SELECT 
                                    T0.""U_SOL_TIKET_WB"",
                                    T0.""U_SOL_STATUS"",
                                    COALESCE(T0.""U_SOL_NOPOL"", '') AS ""U_SOL_NOPOL"",
                                    COALESCE(T0.""U_SOL_NAMA_SUPIR"", '') AS ""U_SOL_NAMA_SUPIR"",
                                    COALESCE(T0.""U_SOL_DOC_TYPE"", '') AS ""U_SOL_DOC_TYPE"",
                                    COALESCE(T0.""U_SOL_DOCNUM_SAP"", '') AS ""U_SOL_DOCNUM_SAP"",
                                    COALESCE(T0.""U_SOL_NO_KONTRAK"", '') AS ""U_SOL_NO_KONTRAK"",
                                    COALESCE(T0.""U_SOL_VENDOR_REF"", '') AS ""U_SOL_VENDOR_REF"",
                                    COALESCE(T0.""U_SOL_COMMODITY"", '') AS ""U_SOL_COMMODITY"",
                                    COALESCE(T0.""U_SOL_BERAT_MASUK"", 0) AS ""U_SOL_BERAT_MASUK"",
                                    TO_VARCHAR(T0.""U_SOL_TANGGAL_MASUK"", 'DD-MM-YYYY') AS ""U_SOL_TANGGAL_MASUK""
                                FROM ""@SOL_WB_DATA"" T0
                                WHERE T0.""U_SOL_STATUS"" = 'WEIGHIN' 
                                   OR T0.""U_SOL_TANGGAL_MASUK"" >= ADD_DAYS(CURRENT_DATE, -60)
                                ORDER BY T0.""Code"" DESC";

                using (var cmd = new HanaCommand(query, connection))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string tiket = rdr["U_SOL_TIKET_WB"] != null ? rdr["U_SOL_TIKET_WB"].ToString().Trim() : "";
                        if (string.IsNullOrEmpty(tiket) || seen.Contains(tiket)) continue;
                        seen.Add(tiket);

                        list.Add(new TicketLookupItem
                        {
                            TiketNum = tiket,
                            Status = rdr["U_SOL_STATUS"] != null ? rdr["U_SOL_STATUS"].ToString().Trim() : "",
                            Nopol = rdr["U_SOL_NOPOL"] != null ? rdr["U_SOL_NOPOL"].ToString().Trim() : "",
                            NamaSupir = rdr["U_SOL_NAMA_SUPIR"] != null ? rdr["U_SOL_NAMA_SUPIR"].ToString().Trim() : "",
                            DocType = rdr["U_SOL_DOC_TYPE"] != null ? rdr["U_SOL_DOC_TYPE"].ToString().Trim() : "",
                            DocNum = rdr["U_SOL_DOCNUM_SAP"] != null ? rdr["U_SOL_DOCNUM_SAP"].ToString().Trim() : "",
                            ContractNo = rdr["U_SOL_NO_KONTRAK"] != null ? rdr["U_SOL_NO_KONTRAK"].ToString().Trim() : "",
                            VendorRef = rdr["U_SOL_VENDOR_REF"] != null ? rdr["U_SOL_VENDOR_REF"].ToString().Trim() : "",
                            Commodity = rdr["U_SOL_COMMODITY"] != null ? rdr["U_SOL_COMMODITY"].ToString().Trim() : "",
                            BeratMasuk = DataConversionHelper.SafeToDouble(rdr["U_SOL_BERAT_MASUK"]),
                            TanggalMasuk = rdr["U_SOL_TANGGAL_MASUK"] != null ? rdr["U_SOL_TANGGAL_MASUK"].ToString() : ""
                        });
                    }
                }
            }
            return list;
        }

        public (string itemCode, string whsCode) GetItemAndWhsCodeFromSO(int docEntry, int lineNum)
        {
            string itemCode = "";
            string whsCode = "";

            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = $"CALL SOL_SP_ADDON_WB_GET_ITEMCODE_WHSCODE_RDR1({docEntry}, {lineNum})";
                using (var command = new HanaCommand(queryString, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        itemCode = reader["ITEMCODE"].ToString();
                        whsCode = reader["WHSCODE"].ToString();
                    }
                }
            }

            return (itemCode, whsCode);
        }

        public (string cardCode, string itemCode) GetPOVendorAndItem(int docEntry, int lineNum)
        {
            string cardCode = "";
            string itemCode = "";

            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = $"SELECT T0.\"CardCode\", T1.\"ItemCode\" FROM \"OPOR\" T0 " +
                                     $"INNER JOIN \"POR1\" T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                                     $"WHERE T0.\"DocEntry\" = {docEntry} AND T1.\"LineNum\" = {lineNum}";
                using (var command = new HanaCommand(queryString, connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        cardCode = reader["CardCode"]?.ToString() ?? "";
                        itemCode = reader["ItemCode"]?.ToString() ?? "";
                    }
                }
            }

            return (cardCode, itemCode);
        }

        public List<(int DocEntry, string DocNum, int LineNum, double OpenQty)> GetOtherOpenLinesForContract(string contractNo, int excludeDocEntry, int excludeLineNum)
        {
            var list = new List<(int DocEntry, string DocNum, int LineNum, double OpenQty)>();
            if (string.IsNullOrWhiteSpace(contractNo)) return list;

            try
            {
                using (var connection = new HanaConnection(GetConnectionString()))
                {
                    connection.Open();
                    string safeContract = contractNo.Trim().Replace("'", "''");
                    string splitQuery = $"SELECT T0.\"DocEntry\", T0.\"DocNum\", T1.\"LineNum\", T1.\"OpenQty\" " +
                                        $"FROM \"OPOR\" T0 INNER JOIN \"POR1\" T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                                        $"WHERE (T0.\"U_SOL_NO_KONTRAK\" = '{safeContract}' OR T0.\"NumAtCard\" = '{safeContract}') " +
                                        $"  AND T0.\"DocStatus\" = 'O' AND T1.\"LineStatus\" = 'O' AND T1.\"OpenQty\" > 0 " +
                                        $"  AND NOT (T0.\"DocEntry\" = {excludeDocEntry} AND T1.\"LineNum\" = {excludeLineNum}) " +
                                        $"ORDER BY T0.\"DocEntry\" ASC, T1.\"LineNum\" ASC";

                    using (var cmdSplit = new HanaCommand(splitQuery, connection))
                    using (var rdrSplit = cmdSplit.ExecuteReader())
                    {
                        while (rdrSplit.Read())
                        {
                            int docEntry = DataConversionHelper.SafeToInt(rdrSplit["DocEntry"]);
                            string docNum = rdrSplit["DocNum"]?.ToString() ?? "";
                            int lineNum = DataConversionHelper.SafeToInt(rdrSplit["LineNum"]);
                            double openQty = DataConversionHelper.SafeToDouble(rdrSplit["OpenQty"]);
                            list.Add((docEntry, docNum, lineNum, openQty));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetOtherOpenLinesForContract error: " + ex.Message);
            }
            return list;
        }

        public List<(string BatchNum, double Qty)> GetAvailableBatches(string itemCode, string whsCode)
        {
            var list = new List<(string BatchNum, double Qty)>();
            if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(whsCode)) return list;

            try
            {
                using (var connection = new HanaConnection(GetConnectionString()))
                {
                    connection.Open();
                    string safeItem = itemCode.Trim().Replace("'", "''");
                    string safeWhs = whsCode.Trim().Replace("'", "''");
                    string query = $"CALL SOL_SP_ADDON_WB_GET_BATCHNUM_QTY('{safeItem}', '{safeWhs}')";

                    using (var cmd = new HanaCommand(query, connection))
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string batchNum = rdr["BATCHNUM"]?.ToString() ?? "";
                            double qty = DataConversionHelper.SafeToDouble(rdr["QTY"]);
                            list.Add((batchNum, qty));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetAvailableBatches error: " + ex.Message);
            }
            return list;
        }

        public DataTable SearchBaseDocGrpoDo(string docType, string docNum)
        {
            var dt = new DataTable();
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string queryString = $"CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC_GRPO_DO('{docType}','{docNum}')";
                using (var command = new HanaCommand(queryString, connection))
                using (var adapter = new HanaDataAdapter(command))
                {
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        public (bool Success, string FullName, string Role, string ErrorMessage) AuthenticateUser(string username, string password)
        {
            try
            {
                using (var connection = new HanaConnection(GetConnectionString()))
                {
                    connection.Open();
                    string safeUser = username.Trim().Replace("'", "''");
                    string safePass = password.Trim().Replace("'", "''");
                    string queryString = $"CALL SOL_SP_ADDON_WB_LOGIN_GET_ROLE('{safeUser}','{safePass}')";

                    using (var command = new HanaCommand(queryString, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            return (false, "", "", "User atau Password salah! Periksa kembali kredensial Anda.");
                        }

                        if (reader.Read())
                        {
                            string role = reader["USERROLE"]?.ToString() ?? "";
                            string fullName = reader["NAME"]?.ToString() ?? "";
                            return (true, fullName, role, "");
                        }
                    }
                }
                return (false, "", "", "Kredensial tidak ditemukan.");
            }
            catch (Exception ex)
            {
                return (false, "", "", "Gagal terhubung ke SAP HANA: " + ex.Message);
            }
        }

        public List<string> GetCommodityList()
        {
            var list = new List<string>();
            try
            {
                using (var connection = new HanaConnection(GetConnectionString()))
                {
                    connection.Open();
                    string queryString = "CALL SOL_SP_ADDON_WB_COMMODITY_LIST()";
                    using (var command = new HanaCommand(queryString, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string commodity = reader["COMMODITY"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(commodity))
                            {
                                list.Add(commodity);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetCommodityList error: " + ex.Message);
            }
            return list;
        }

        public DataTable GetReportData(string noTiket, string noPolisi, string commodity, string fromDate, string toDate)
        {
            var dt = new DataTable();
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string safeTiket = (noTiket ?? "").Trim().Replace("'", "''");
                string safeNopol = (noPolisi ?? "").Trim().Replace("'", "''");
                string safeCommodity = (commodity ?? "").Trim().Replace("'", "''");
                string safeFrom = (fromDate ?? "").Trim().Replace("'", "''");
                string safeTo = (toDate ?? "").Trim().Replace("'", "''");

                string queryString = $"CALL SOL_SP_ADDON_WB_REPORT('{safeTiket}', '{safeNopol}', '{safeCommodity}', '{safeFrom}', '{safeTo}')";
                using (var command = new HanaCommand(queryString, connection))
                using (var adapter = new HanaDataAdapter(command))
                {
                    adapter.Fill(dt);
                }

                if (!dt.Columns.Contains("Nama Vendor"))
                {
                    dt.Columns.Add("Nama Vendor", typeof(string));
                    if (dt.Columns.Contains("Vendor Ref No. SAP"))
                    {
                        int targetIdx = dt.Columns["Vendor Ref No. SAP"].Ordinal + 1;
                        dt.Columns["Nama Vendor"].SetOrdinal(targetIdx);
                    }
                }
            }
            return dt;
        }

        public DataTable GetPrintTicketData(string ticketNumber)
        {
            var dt = new DataTable();
            using (var connection = new HanaConnection(GetConnectionString()))
            {
                connection.Open();
                string safeTiket = (ticketNumber ?? "").Trim().Replace("'", "''");
                string queryString = $"CALL SOL_SP_ADDON_WB_PRINT_TIKET('{safeTiket}')";
                using (var command = new HanaCommand(queryString, connection))
                using (var adapter = new HanaDataAdapter(command))
                {
                    adapter.Fill(dt);
                }

                if (dt.Rows.Count > 0)
                {
                    if (!dt.Columns.Contains("Vendor Ref"))
                    {
                        dt.Columns.Add("Vendor Ref", typeof(string));
                    }

                    var vendorRefs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var vendorRefsByRow = new List<string>();
                    try
                    {
                        string wbQuery = $"SELECT \"U_SOL_DOCNUM_SAP\", \"U_SOL_NO_KONTRAK\", COALESCE(\"U_SOL_VENDOR_REF\", '') AS \"U_SOL_VENDOR_REF\" FROM \"@SOL_WB_DATA\" WHERE \"U_SOL_TIKET_WB\" = '{safeTiket}'";
                        using (var wbCmd = new HanaCommand(wbQuery, connection))
                        using (var rdr = wbCmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                string docNum = rdr["U_SOL_DOCNUM_SAP"]?.ToString().Trim() ?? "";
                                string kontrak = rdr["U_SOL_NO_KONTRAK"]?.ToString().Trim() ?? "";
                                string vRef = rdr["U_SOL_VENDOR_REF"]?.ToString().Trim() ?? "";
                                if (!string.IsNullOrEmpty(docNum) && !string.IsNullOrEmpty(vRef) && !vendorRefs.ContainsKey(docNum))
                                {
                                    vendorRefs[docNum] = vRef;
                                }
                                if (!string.IsNullOrEmpty(kontrak) && !string.IsNullOrEmpty(vRef) && !vendorRefs.ContainsKey(kontrak))
                                {
                                    vendorRefs[kontrak] = vRef;
                                }
                                vendorRefsByRow.Add(vRef);
                            }
                        }
                    }
                    catch { }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var row = dt.Rows[i];
                        string cNo = dt.Columns.Contains("Contract No") ? row["Contract No"]?.ToString().Trim() ?? "" : "";
                        string doNo = dt.Columns.Contains("Do No") ? row["Do No"]?.ToString().Trim() ?? "" : "";
                        string vRef = "";

                        if (!string.IsNullOrEmpty(cNo) && vendorRefs.ContainsKey(cNo))
                        {
                            vRef = vendorRefs[cNo];
                        }
                        else if (!string.IsNullOrEmpty(doNo) && vendorRefs.ContainsKey(doNo))
                        {
                            vRef = vendorRefs[doNo];
                        }
                        else if (i < vendorRefsByRow.Count && !string.IsNullOrEmpty(vendorRefsByRow[i]))
                        {
                            vRef = vendorRefsByRow[i];
                        }

                        if (string.IsNullOrEmpty(vRef) && !string.IsNullOrEmpty(cNo))
                        {
                            try
                            {
                                string oporQuery = $"SELECT COALESCE(\"NumAtCard\", '') FROM OPOR WHERE \"DocNum\" = '{cNo.Replace("'", "''")}'";
                                using (var oporCmd = new HanaCommand(oporQuery, connection))
                                {
                                    var oporRes = oporCmd.ExecuteScalar();
                                    if (oporRes != null && oporRes != DBNull.Value)
                                    {
                                        vRef = oporRes.ToString().Trim();
                                    }
                                }
                            }
                            catch { }
                        }

                        row["Vendor Ref"] = vRef;
                    }
                }
            }
            dt.TableName = "SOL_SP_ADDON_WB_PRINT_TIKET";
            return dt;
        }
    }
}
