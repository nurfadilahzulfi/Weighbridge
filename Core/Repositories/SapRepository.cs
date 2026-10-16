using SAPbobsCOM;
using System;
using System.Collections.Generic;
using Weighbridge.Connection;
using Weighbridge.Core.Config;
using Weighbridge.Core.Models;

namespace Weighbridge.Core.Repositories
{
    public class SapRepository : ISapRepository
    {
        private readonly SBOConnection _sboConn;
        private readonly IHanaRepository _hanaRepo;

        public SapRepository() : this(new SBOConnection(), new HanaRepository())
        {
        }

        public SapRepository(SBOConnection sboConn) : this(sboConn, new HanaRepository())
        {
        }

        public SapRepository(SBOConnection sboConn, IHanaRepository hanaRepo)
        {
            _sboConn = sboConn ?? new SBOConnection();
            _hanaRepo = hanaRepo ?? new HanaRepository();
        }

        private Company Connect()
        {
            _sboConn.connectSBOS(ConfigManager.sboConfig);
            return _sboConn.oCompany;
        }

        private void Disconnect(Company company)
        {
            try
            {
                if (company != null && company.Connected)
                    company.Disconnect();
            }
            catch { }
        }

        public bool SaveWeighInUDO(WeighInData data, out string error)
        {
            error = "";
            Company company = null;
            try
            {
                company = Connect();
                CompanyService companyService = company.GetCompanyService();
                GeneralService generalService = companyService.GetGeneralService("OWB");
                GeneralData generalData = (GeneralData)generalService.GetDataInterface(GeneralServiceDataInterfaces.gsGeneralData);

                generalData.SetProperty("Code", data.Code ?? "");
                generalData.SetProperty("U_SOL_DOC_TYPE", data.DocType ?? "PO");
                generalData.SetProperty("U_SOL_DOCNUM_SAP", data.DocNum ?? "");
                generalData.SetProperty("U_SOL_VENDOR_REF", data.VendorRef ?? "");
                generalData.SetProperty("U_SOL_DOC_ENTRY_SAP", data.DocEntry);
                generalData.SetProperty("U_SOL_TIKET_WB", data.TiketNum ?? "");
                generalData.SetProperty("U_SOL_COMMODITY", data.Commodity ?? "");
                generalData.SetProperty("U_SOL_NO_KONTRAK", data.NoKontrak ?? "");
                generalData.SetProperty("U_SOL_NOPOL", data.Nopol ?? "");
                generalData.SetProperty("U_SOL_NAMA_SUPIR", data.NamaSupir ?? "");
                generalData.SetProperty("U_SOL_NO_SIM_SUPIR", data.NoSimSupir ?? "");
                generalData.SetProperty("U_SOL_JENIS_TRUCK", data.JenisTruck ?? "");
                generalData.SetProperty("U_SOL_BERAT_MASUK", data.BeratMasuk);
                generalData.SetProperty("U_SOL_BRUTO_KEBUN", data.BrutoKebun.ToString(System.Globalization.CultureInfo.InvariantCulture));
                generalData.SetProperty("U_SOL_TARA_KEBUN", data.TaraKebun.ToString(System.Globalization.CultureInfo.InvariantCulture));
                generalData.SetProperty("U_SOL_NETTO_KEBUN", data.NettoKebun.ToString(System.Globalization.CultureInfo.InvariantCulture));
                generalData.SetProperty("U_SOL_FFA_KEBUN", data.FfaKebun ?? "");
                generalData.SetProperty("U_SOL_MOIST_KEBUN", data.MoistKebun ?? "");
                generalData.SetProperty("U_SOL_LAB_FFA", data.LabFfa ?? "");
                generalData.SetProperty("U_SOL_LAB_MOIST", data.LabMoist ?? "");
                generalData.SetProperty("U_SOL_LAB_IMP", data.LabImp ?? "");
                generalData.SetProperty("U_SOL_LAB_DOBI", data.LabDobi ?? "");
                generalData.SetProperty("U_SOL_LAB_CAROTINE", data.LabCarotine ?? "");
                generalData.SetProperty("U_SOL_KODE_DOKUMEN", data.KodeDokumenPabean ?? "");
                generalData.SetProperty("U_SOL_NO_DOKUMEN", data.NoDokumenPabean ?? "");
                generalData.SetProperty("U_SOL_NOMOR_SPB", data.NomorSpb ?? "");
                generalData.SetProperty("U_SOL_NOMOR_AJU", data.NomorAju ?? "");
                generalData.SetProperty("U_SOL_SEAL_NO", data.SealNo ?? "");
                generalData.SetProperty("U_SOL_DO_NO", data.DoNo ?? "");
                generalData.SetProperty("U_SOL_CONTAINER_1_NO", data.Container1No ?? "");
                generalData.SetProperty("U_SOL_CONTAINER_2_NO", data.Container2No ?? "");
                generalData.SetProperty("U_SOL_NOTE", data.Note ?? "");
                generalData.SetProperty("U_SOL_USER", data.Username ?? "");
                generalData.SetProperty("U_SOL_STATUS", data.Status ?? "WEIGHIN");
                generalData.SetProperty("U_SOL_LINENUM_SAP", data.LineNum);
                generalData.SetProperty("U_SOL_TANGGAL_MASUK", (data.TanggalMasuk ?? DateTime.Today).Date);
                generalData.SetProperty("U_SOL_WAKTU_MASUK", data.WaktuMasuk ?? DateTime.Now);

                try { if (data.TanggalSpb.HasValue) generalData.SetProperty("U_SOL_TANGGAL_SPB", Convert.ToDateTime(data.TanggalSpb.Value)); } catch { }
                try { if (!string.IsNullOrEmpty(data.StatusJalur)) generalData.SetProperty("U_SOL_STATUS_JALUR", data.StatusJalur); } catch { }
                try { if (!string.IsNullOrEmpty(data.FasilitasPabean)) generalData.SetProperty("U_SOL_FASILITAS_PABEAN", data.FasilitasPabean); } catch { }

                if (!string.IsNullOrEmpty(data.TransporterInt))
                {
                    generalData.SetProperty("U_SOL_TRANSPORTER_INT", data.TransporterInt);
                    generalData.SetProperty("U_SOL_TRANSPORTER_EKS", "");
                }
                else if (!string.IsNullOrEmpty(data.TransporterEks))
                {
                    generalData.SetProperty("U_SOL_TRANSPORTER_INT", "");
                    generalData.SetProperty("U_SOL_TRANSPORTER_EKS", data.TransporterEks);
                }

                if (data.TanggalDokumenPabean.HasValue)
                    generalData.SetProperty("U_SOL_TANGGAL_DOKUMEN", Convert.ToDateTime(data.TanggalDokumenPabean.Value));

                generalService.Add(generalData);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                Disconnect(company);
            }
        }

        public bool UpdateWeighOutUDO(WeighOutData data, out string error)
        {
            error = "";
            if (data == null)
            {
                error = "Data timbang keluar null.";
                return false;
            }

            Company company = null;
            try
            {
                company = Connect();
                CompanyService companyService = company.GetCompanyService();
                GeneralService generalService = companyService.GetGeneralService("OWB");
                GeneralDataParams generalParams = (GeneralDataParams)generalService.GetDataInterface(GeneralServiceDataInterfaces.gsGeneralDataParams);
                generalParams.SetProperty("Code", data.Code ?? "");

                GeneralData generalData = generalService.GetByParams(generalParams);

                generalData.SetProperty("U_SOL_BERAT_KELUAR", data.BeratKeluar);
                generalData.SetProperty("U_SOL_BERAT_NETTO", data.BeratNetto);
                generalData.SetProperty("U_SOL_TANGGAL_KELUAR", Convert.ToDateTime(DateTime.Today));
                generalData.SetProperty("U_SOL_WAKTU_KELUAR", DateTime.Now);
                generalData.SetProperty("U_SOL_STATUS", string.IsNullOrEmpty(data.Status) ? "SYNCSAP" : data.Status);
                generalData.SetProperty("U_SOL_WEIGHOUT", string.IsNullOrEmpty(data.WeighoutFlag) ? "Y" : data.WeighoutFlag);

                if (!string.IsNullOrEmpty(data.TangkiNo))
                    generalData.SetProperty("U_SOL_NOMOR_TANGKI", data.TangkiNo);

                generalData.SetProperty("U_SOL_LAB_FFA", data.LabFfa ?? "");
                generalData.SetProperty("U_SOL_LAB_MOIST", data.LabMoist ?? "");
                generalData.SetProperty("U_SOL_LAB_IMP", data.LabImp ?? "");
                generalData.SetProperty("U_SOL_LAB_DOBI", data.LabDobi ?? "");
                generalData.SetProperty("U_SOL_LAB_CAROTINE", data.LabCarotine ?? "");

                // Koreksi data kebun dari timbang keluar (mis. Tara/Netto tertukar saat timbang masuk)
                if (!string.IsNullOrEmpty(data.BrutoKebun)) generalData.SetProperty("U_SOL_BRUTO_KEBUN", data.BrutoKebun);
                if (!string.IsNullOrEmpty(data.TaraKebun)) generalData.SetProperty("U_SOL_TARA_KEBUN", data.TaraKebun);
                if (!string.IsNullOrEmpty(data.NettoKebun)) generalData.SetProperty("U_SOL_NETTO_KEBUN", data.NettoKebun);
                if (!string.IsNullOrEmpty(data.FfaKebun)) generalData.SetProperty("U_SOL_FFA_KEBUN", data.FfaKebun);
                if (!string.IsNullOrEmpty(data.MoistKebun)) generalData.SetProperty("U_SOL_MOIST_KEBUN", data.MoistKebun);
                if (!string.IsNullOrEmpty(data.SealNo)) generalData.SetProperty("U_SOL_SEAL_NO", data.SealNo);
                if (!string.IsNullOrEmpty(data.DoNo)) generalData.SetProperty("U_SOL_DO_NO", data.DoNo);
                if (!string.IsNullOrEmpty(data.Note)) generalData.SetProperty("U_SOL_NOTE", data.Note);
                if (!string.IsNullOrEmpty(data.Container1No)) generalData.SetProperty("U_SOL_CONTAINER_1_NO", data.Container1No);
                if (!string.IsNullOrEmpty(data.Container2No)) generalData.SetProperty("U_SOL_CONTAINER_2_NO", data.Container2No);

                generalService.Update(generalData);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                Disconnect(company);
            }
        }

        public bool UpdateWeighOutUDO(string code, double beratKeluar, double beratNetto, string status, string weighout, string tangkiNo, out string error)
        {
            var data = new WeighOutData
            {
                Code = code ?? "",
                BeratKeluar = beratKeluar,
                BeratNetto = beratNetto,
                Status = status ?? "SYNCSAP",
                WeighoutFlag = weighout ?? "Y",
                TangkiNo = tangkiNo ?? ""
            };
            return UpdateWeighOutUDO(data, out error);
        }

        public bool CreateGRPO(WeighOutData data, out int createdDocEntry, out string error)
        {
            createdDocEntry = 0;
            error = "";
            Company company = null;

            try
            {
                company = Connect();
                company.StartTransaction();

                Documents oGRPO = (Documents)company.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
                SetGRPOHeader(oGRPO, data);

                var reserve = _hanaRepo.SearchPOSOReserve(data.DocType, data.DocEntry);
                if (reserve.isIns == "Y" && reserve.baseType == 22)
                {
                    oGRPO.Lines.BaseType = 18;
                    oGRPO.Lines.BaseEntry = reserve.docEntry;
                    oGRPO.Lines.BaseLine = reserve.lineNum;
                    oGRPO.Lines.InventoryQuantity = data.BeratNetto;
                    SetLineWeighbridgeQty(oGRPO.Lines, data.BeratNetto);
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = data.BrutoKebun ?? "";
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = data.TaraKebun ?? "";
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = data.NettoKebun ?? "";
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = data.FfaKebun ?? "";
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = data.MoistKebun ?? "";
                    oGRPO.Lines.WarehouseCode = data.TangkiNo ?? "";

                    oGRPO.Lines.BatchNumbers.BatchNumber = data.TiketNum ?? "";
                    oGRPO.Lines.BatchNumbers.Quantity = data.BeratNetto;
                    oGRPO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;
                    oGRPO.Lines.BatchNumbers.Add();
                    oGRPO.Lines.Add();
                }
                else
                {
                    double baseOpenQty = 0;
                    var freshQuota = _hanaRepo.GetFreshQuota(data.DocType, data.DocEntry, data.LineNum);
                    baseOpenQty = freshQuota.openQty;
                    string contractNo = data.NoKontrak?.Trim() ?? "";

                    if (baseOpenQty <= 0 || data.BeratNetto <= baseOpenQty || string.IsNullOrEmpty(contractNo))
                    {
                        oGRPO.Lines.BaseType = 22;
                        oGRPO.Lines.BaseEntry = data.DocEntry;
                        oGRPO.Lines.BaseLine = data.LineNum;
                        oGRPO.Lines.InventoryQuantity = data.BeratNetto;
                        SetLineWeighbridgeQty(oGRPO.Lines, data.BeratNetto);
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = data.BrutoKebun ?? "";
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = data.TaraKebun ?? "";
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = data.NettoKebun ?? "";
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = data.FfaKebun ?? "";
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = data.MoistKebun ?? "";
                        oGRPO.Lines.WarehouseCode = data.TangkiNo ?? "";

                        oGRPO.Lines.BatchNumbers.BatchNumber = data.TiketNum ?? "";
                        oGRPO.Lines.BatchNumbers.Quantity = data.BeratNetto;
                        oGRPO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;
                        oGRPO.Lines.BatchNumbers.Add();
                        oGRPO.Lines.Add();
                    }
                    else
                    {
                        // Isi baris PO lain dalam kontrak yang sama lebih dulu; sisa yang tetap tidak muat (surplus)
                        // masuk ke baris PO utama sebagai kelebihan terima, seperti truk non-split lainnya.
                        double remainingQty = data.BeratNetto - baseOpenQty;
                        var otherAllocs = new System.Collections.Generic.List<(int DocEntry, int LineNum, double Qty)>();
                        foreach (var other in _hanaRepo.GetOtherOpenLinesForContract(contractNo, data.DocEntry, data.LineNum))
                        {
                            if (remainingQty <= 0) break;
                            double takeQty = Math.Min(remainingQty, other.OpenQty);
                            otherAllocs.Add((other.DocEntry, other.LineNum, takeQty));
                            remainingQty -= takeQty;
                        }
                        double baseQty = baseOpenQty + Math.Max(0, remainingQty);

                        oGRPO.Lines.BaseType = 22;
                        oGRPO.Lines.BaseEntry = data.DocEntry;
                        oGRPO.Lines.BaseLine = data.LineNum;
                        oGRPO.Lines.InventoryQuantity = baseQty;
                        SetLineWeighbridgeQty(oGRPO.Lines, baseQty);
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = data.BrutoKebun ?? "";
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = data.TaraKebun ?? "";
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = data.NettoKebun ?? "";
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = data.FfaKebun ?? "";
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = data.MoistKebun ?? "";
                        oGRPO.Lines.WarehouseCode = data.TangkiNo ?? "";

                        oGRPO.Lines.BatchNumbers.BatchNumber = data.TiketNum ?? "";
                        oGRPO.Lines.BatchNumbers.Quantity = baseQty;
                        oGRPO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;
                        oGRPO.Lines.BatchNumbers.Add();
                        oGRPO.Lines.Add();

                        foreach (var other in otherAllocs)
                        {
                            oGRPO.Lines.BaseType = 22;
                            oGRPO.Lines.BaseEntry = other.DocEntry;
                            oGRPO.Lines.BaseLine = other.LineNum;
                            oGRPO.Lines.InventoryQuantity = other.Qty;
                            SetLineWeighbridgeQty(oGRPO.Lines, other.Qty);
                            oGRPO.Lines.WarehouseCode = data.TangkiNo;

                            oGRPO.Lines.BatchNumbers.BatchNumber = data.TiketNum;
                            oGRPO.Lines.BatchNumbers.Quantity = other.Qty;
                            oGRPO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;
                            oGRPO.Lines.BatchNumbers.Add();
                            oGRPO.Lines.Add();
                        }
                    }
                }

                int retval = oGRPO.Add();
                if (retval == 0)
                {
                    string newKey = company.GetNewObjectKey();
                    int.TryParse(newKey, out createdDocEntry);

                    if (company.InTransaction)
                        company.EndTransaction(BoWfTransOpt.wf_Commit);

                    return true;
                }
                else
                {
                    error = company.GetLastErrorDescription();
                    if (company.InTransaction)
                        company.EndTransaction(BoWfTransOpt.wf_RollBack);

                    return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                if (company != null && company.InTransaction)
                    company.EndTransaction(BoWfTransOpt.wf_RollBack);

                return false;
            }
            finally
            {
                Disconnect(company);
            }
        }

        public bool CreateSplitGRPO(IList<SplitGRPOPart> parts, out List<int> docEntries, out string error)
        {
            docEntries = new List<int>();
            error = "";
            if (parts == null || parts.Count == 0)
            {
                error = "Tidak ada bagian kontrak untuk GRPO split.";
                return false;
            }

            Company company = null;
            try
            {
                company = Connect();
                company.StartTransaction();

                for (int i = 0; i < parts.Count; i++)
                {
                    WeighOutData data = parts[i].Data;
                    double netto = parts[i].Netto;
                    string label = "K" + (i + 1);
                    if (data == null || netto < 1.0)
                    {
                        docEntries.Add(0);
                        continue;
                    }

                    Documents oGRPO = (Documents)company.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
                    SetGRPOHeader(oGRPO, data);
                    try { oGRPO.UserFields.Fields.Item("U_SOL_QTY_WB").Value = netto; } catch { }
                    oGRPO.Comments = $"Split Kontrak {label} - Tiket: {data.TiketNum} ({label} PO: {data.DocNum}, Alokasi: {netto:N0} KG)";

                    oGRPO.Lines.BaseType = 22;
                    oGRPO.Lines.BaseEntry = data.DocEntry;
                    oGRPO.Lines.BaseLine = data.LineNum;
                    oGRPO.Lines.InventoryQuantity = netto;
                    SetLineWeighbridgeQty(oGRPO.Lines, netto);
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = data.BrutoKebun ?? "";
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = data.TaraKebun ?? "";
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = data.NettoKebun ?? "";
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = data.FfaKebun ?? "";
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = data.MoistKebun ?? "";
                    oGRPO.Lines.WarehouseCode = data.TangkiNo ?? "";

                    oGRPO.Lines.BatchNumbers.BatchNumber = data.TiketNum ?? "";
                    oGRPO.Lines.BatchNumbers.Quantity = netto;
                    oGRPO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;
                    oGRPO.Lines.BatchNumbers.Add();
                    oGRPO.Lines.Add();

                    if (oGRPO.Add() != 0)
                    {
                        error = $"Gagal membuat GRPO {label}: {company.GetLastErrorDescription()}";
                        company.EndTransaction(BoWfTransOpt.wf_RollBack);
                        return false;
                    }
                    int.TryParse(company.GetNewObjectKey(), out int docEntry);
                    docEntries.Add(docEntry);
                }

                if (docEntries.TrueForAll(d => d == 0))
                {
                    error = "Tidak ada GRPO yang dibuat. Periksa nilai netto tiap kontrak.";
                    if (company.InTransaction) company.EndTransaction(BoWfTransOpt.wf_RollBack);
                    return false;
                }

                if (company.InTransaction)
                    company.EndTransaction(BoWfTransOpt.wf_Commit);

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                if (company != null && company.InTransaction)
                    company.EndTransaction(BoWfTransOpt.wf_RollBack);

                return false;
            }
            finally
            {
                Disconnect(company);
            }
        }

        public bool CreateDeliveryOrder(WeighOutData data, string itemCode, string whsCode, out int createdDocEntry, out string error)
        {
            createdDocEntry = 0;
            error = "";
            Company company = null;

            try
            {
                company = Connect();
                company.StartTransaction();

                Documents oDO = (Documents)company.GetBusinessObject(BoObjectTypes.oDeliveryNotes);
                SetGRPOHeader(oDO, data);

                var reserve = _hanaRepo.SearchPOSOReserve(data.DocType, data.DocEntry);
                bool isReserve = (reserve.isIns == "Y" && reserve.baseType == 17);

                oDO.Lines.BaseType = isReserve ? 13 : 17;
                oDO.Lines.BaseEntry = isReserve ? reserve.docEntry : data.DocEntry;
                oDO.Lines.BaseLine = isReserve ? reserve.lineNum : data.LineNum;
                oDO.Lines.InventoryQuantity = data.BeratNetto;
                SetLineWeighbridgeQty(oDO.Lines, data.BeratNetto);
                oDO.Lines.WarehouseCode = data.TangkiNo ?? "";
                oDO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = data.BrutoKebun ?? "";
                oDO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = data.TaraKebun ?? "";
                oDO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = data.NettoKebun ?? "";
                oDO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = data.FfaKebun ?? "";
                oDO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = data.MoistKebun ?? "";

                var batches = _hanaRepo.GetAvailableBatches(itemCode, whsCode);
                double remainingQty = data.BeratNetto;
                foreach (var batch in batches)
                {
                    if (remainingQty <= 0) break;
                    double takeQty = batch.Qty >= remainingQty ? remainingQty : batch.Qty;

                    oDO.Lines.BatchNumbers.BatchNumber = batch.BatchNum;
                    oDO.Lines.BatchNumbers.Quantity = takeQty;
                    oDO.Lines.BatchNumbers.Add();

                    remainingQty -= takeQty;
                }

                if (remainingQty > 0 && batches.Count == 0)
                {
                    oDO.Lines.BatchNumbers.BatchNumber = data.TiketNum;
                    oDO.Lines.BatchNumbers.Quantity = data.BeratNetto;
                    oDO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;
                    oDO.Lines.BatchNumbers.Add();
                }

                oDO.Lines.Add();

                int retval = oDO.Add();
                if (retval == 0)
                {
                    int.TryParse(company.GetNewObjectKey(), out createdDocEntry);
                    if (company.InTransaction)
                        company.EndTransaction(BoWfTransOpt.wf_Commit);
                    return true;
                }
                else
                {
                    error = company.GetLastErrorDescription();
                    if (company.InTransaction)
                        company.EndTransaction(BoWfTransOpt.wf_RollBack);
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                if (company != null && company.InTransaction)
                    company.EndTransaction(BoWfTransOpt.wf_RollBack);
                return false;
            }
            finally
            {
                Disconnect(company);
            }
        }

        public bool SetUDOReprintFlag(string code, out string error)
        {
            error = "";
            Company company = null;
            try
            {
                company = Connect();
                CompanyService companyService = company.GetCompanyService();
                GeneralService generalService = companyService.GetGeneralService("OWB");
                GeneralDataParams generalParams = (GeneralDataParams)generalService.GetDataInterface(GeneralServiceDataInterfaces.gsGeneralDataParams);
                generalParams.SetProperty("Code", code);

                GeneralData generalData = generalService.GetByParams(generalParams);
                generalData.SetProperty("U_SOL_REPRINT", "Y");
                generalService.Update(generalData);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                Disconnect(company);
            }
        }

        public bool UpdateGrpoLineWeight(int docEntry, int lineNum, double qtyNetto, out string error)
        {
            error = "";
            Company company = null;
            try
            {
                company = Connect();
                Documents oGRPO = (Documents)company.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
                if (!oGRPO.GetByKey(docEntry))
                {
                    error = "GRPO with DocEntry " + docEntry + " not found.";
                    return false;
                }

                oGRPO.Lines.SetCurrentLine(lineNum);
                oGRPO.Lines.UserFields.Fields.Item("U_SOL_QTY_WB").Value = qtyNetto;

                int retval = oGRPO.Update();
                if (retval != 0)
                {
                    error = company.GetLastErrorDescription();
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                Disconnect(company);
            }
        }

        public bool UpdateDoLineWeight(int docEntry, int lineNum, double qtyNetto, out string error)
        {
            error = "";
            Company company = null;
            try
            {
                company = Connect();
                Documents oDO = (Documents)company.GetBusinessObject(BoObjectTypes.oDeliveryNotes);
                if (!oDO.GetByKey(docEntry))
                {
                    error = "DO with DocEntry " + docEntry + " not found.";
                    return false;
                }

                oDO.Lines.SetCurrentLine(lineNum);
                oDO.Lines.UserFields.Fields.Item("U_SOL_QTY_WB").Value = qtyNetto;

                int retval = oDO.Update();
                if (retval != 0)
                {
                    error = company.GetLastErrorDescription();
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                Disconnect(company);
            }
        }

        private static void SetLineWeighbridgeQty(Document_Lines lines, double qty)
        {
            lines.UserFields.Fields.Item("U_SOL_OPENQTY").Value = qty;
            try { lines.UserFields.Fields.Item("U_SOL_QTY_WB").Value = qty; } catch { }
        }
        private void SetGRPOHeader(Documents doc, WeighOutData data)
        {
            doc.DocDate = DateTime.Today;
            string kodePabean = data.KodeDokumenPabean?.Length >= 7 ? data.KodeDokumenPabean.Substring(0, 7) : (data.KodeDokumenPabean ?? string.Empty);
            doc.UserFields.Fields.Item("U_SOL_KODE_DOKUMEN").Value = kodePabean;
            doc.UserFields.Fields.Item("U_SOL_NOMOR_DOKUMEN").Value = data.NoDokumenPabean ?? "";
            doc.UserFields.Fields.Item("U_SOL_TANGGAL_DOKUMEN").Value = data.TanggalDokumenPabean ?? DateTime.Today;
            doc.UserFields.Fields.Item("U_SOL_NO_TIKET_WB").Value = data.TiketNum ?? "";
            doc.UserFields.Fields.Item("U_SOL_NO_SPB").Value = data.NomorSpb ?? "";
            doc.UserFields.Fields.Item("U_SOL_NOMOR_AJU").Value = data.NomorAju ?? "";
            doc.UserFields.Fields.Item("U_SOL_NAMA_SUPIR").Value = data.NamaSupir ?? "";
            doc.UserFields.Fields.Item("U_SOL_JENIS_TRUCK").Value = data.JenisTruck ?? "";
            doc.UserFields.Fields.Item("U_SOL_NO_PLAT").Value = data.Nopol ?? "";

            if (!string.IsNullOrEmpty(data.TransporterInt))
                doc.UserFields.Fields.Item("U_SOL_VENDOR").Value = data.TransporterInt;
            else if (!string.IsNullOrEmpty(data.TransporterEks))
                doc.UserFields.Fields.Item("U_SOL_NAMA_TRANSPORTER").Value = data.TransporterEks;

            try { doc.UserFields.Fields.Item("U_SOL_QTY_WB").Value = data.BeratNetto; } catch { }
        }
    }
}