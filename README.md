# Weighbridge

Aplikasi desktop Windows untuk operasional **jembatan timbang (weighbridge)** di PT Tanimas Resources International. Aplikasi membaca berat truk langsung dari indikator timbangan lewat port serial, mencatat tiketnya, lalu menyinkronkan hasilnya ke **SAP Business One (HANA)**: GRPO untuk PO, Delivery untuk SO.

## Fitur

- **Timbang Masuk**: pilih dokumen dasar (PO atau SO), cek sisa kuota, baca berat dari timbangan, simpan tiket, lalu cetak.
- **Timbang Keluar**: ambil tiket yang masih pending, hitung netto (selisih berat masuk dan keluar), buat GRPO atau Delivery Order di SAP, lalu perbarui status tiket.
- **Split Kontrak**: satu truk dibagi ke 2 atau 3 kontrak (K1, K2, K3). Alokasi netto dan GRPO dibuat per kontrak.
- **Validasi kuota PO**: peringatan atau blokir jika berat melebihi sisa open quantity, termasuk kalau ada beberapa truk pending untuk PO yang sama.
- **Data Pabean (Bea Cukai)**: nomor aju, kode dokumen, status jalur (Hijau, Kuning, Merah, MITA), dan fasilitas. Data terisi otomatis dari dokumen SAP dan divalidasi sebelum disimpan.
- **Update GRPO dan Delivery**: sinkronisasi ulang ke SAP berdasarkan dokumen sebelumnya (Timbang Masuk dan Timbang Keluar).
- **Reprint tiket** dan cetak ke printer atau ekspor ke PDF (Crystal Reports).
- **Report** dengan filter komoditas dan tanggal, bisa di-export ke Excel.
- **Login berbasis role**: `SU` (Super User) bisa membuka menu Konfigurasi, role lainnya sebagai Operator.

## Alur kerja

```
Login -> Timbang Masuk -> (truk bongkar/muat) -> Timbang Keluar -> Sync ke SAP -> Cetak tiket
              |                                         |
        simpan ke UDO @SOL_WB_DATA          GRPO (PO) / Delivery (SO) + update status UDO
```

## Teknologi

| Komponen | Detail |
|---|---|
| Bahasa / UI | C#, Windows Forms |
| Framework | .NET Framework 4.7.2 |
| ERP | SAP Business One lewat DI API (`lib/Interop.SAPbobsCOM.dll`) |
| Database | SAP HANA (`Sap.Data.Hana.v4.5`) |
| Laporan / tiket | Crystal Reports 13 |
| Export Excel | EPPlus 5.8.14 |
| Logging | log4net |
| Timbangan | `System.IO.Ports` (RS-232) |
| Test | MSTest (project `Tests/`) |

## Struktur folder

```
Weighbridge/
├── Program.cs, StartUp.cs        Entry point: cek config, login, form utama
├── FormSync.cs                   Form utama dan menu
├── WeighbridgeForm*.cs           Timbang Masuk (termasuk split kontrak)
├── TimbangKeluarForm*.cs         Timbang Keluar (termasuk split tiket dan overflow)
├── *UpdateGRPODOForm.cs          Update GRPO dan Delivery
├── ReportForm.cs, ReprintTiket.cs, SelectTicketForm.cs, ConfigurationForm.cs, LoginForm.cs
├── Core/
│   ├── Config/                   Konfigurasi timbangan, SBO, dan feature flag
│   ├── Services/                 Logika bisnis (WeighIn/Out, Pabean, Print, SerialPort, Excel, Auth, ...)
│   ├── Repositories/             Akses data HANA dan SAP
│   ├── Models/, Helpers/, Encryptions/, XML/
├── Connection/                   Koneksi SAP DI API, HANA, dan serial port timbangan
├── Assets/                       Ikon, logo, dan template tiket (.rpt)
├── lib/                          Interop.SAPbobsCOM.dll
├── Tests/                        Unit test
└── Config.example.xml            Contoh file konfigurasi
```

## Prasyarat

- Windows dengan **.NET Framework 4.7.2**
- **Visual Studio 2019/2022** untuk build
- **SAP HANA Client (ADO.NET)** terpasang. Project merujuk ke `C:\Program Files (x86)\sap\hdbclient\ado.net\v4.5\Sap.Data.Hana.v4.5.dll`
- **SAP Business One DI API** (32/64-bit sesuai platform build)
- **Crystal Reports runtime** untuk cetak tiket
- Akses jaringan ke server HANA dan SAP B1
- UDO dan tabel kustom di SAP B1 (`@SOL_WB_DATA` beserta stored procedure cetaknya)
- Timbangan terhubung lewat port COM

## Build

1. Clone repo:
   ```
   git clone https://github.com/nurfadilahzulfi/Weighbridge.git
   ```
2. Buka `Weighbridge.sln` di Visual Studio.
3. Restore NuGet package (`packages.config`).
4. Pilih konfigurasi dan platform (`Debug|x64` atau `Release|x86` sesuai DI API yang terpasang), lalu build.

## Konfigurasi

Saat pertama dijalankan dan `Config/Config.xml` belum ada, form **Konfigurasi** muncul otomatis. Contoh isi file ada di [Config.example.xml](Config.example.xml):

| Bagian | Isi |
|---|---|
| `WEIGHBRIDGE` | Port COM, baud rate, data bits, timeout baca/tulis, prefix data dari indikator |
| `SBO_DATABASE` | Tipe dan alamat server HANA, nama database SBO, user DB, user dan password SBO |

`Config.xml` dibuat di folder `Config/` di samping file `.exe` dan **diabaikan git** karena memuat kredensial. Jangan commit file itu.

Penimbangan manual dimatikan lewat `AppFeatures.AllowManualWeighing = false` di [Core/Config/AppFeatures.cs](Core/Config/AppFeatures.cs). Berat hanya boleh datang dari timbangan.

## Menjalankan test

Buka solusi di Visual Studio lalu jalankan **Test > Run All Tests**, atau lewat command line:

```
vstest.console.exe Tests\bin\Debug\Weighbridge.Tests.dll
```

Test mencakup perhitungan netto, kuota PO, alokasi split kontrak (2 dan 3 kontrak), validasi Pabean, filter pencarian dokumen dan tiket, mapping kolom cetak, serta hak akses per role.

## Catatan

- Folder `Assets/Report/` berisi template tiket: `WeighbridgeTiket-Masuk.rpt` dan `WeighbridgeTiket-Keluar.rpt`.
- File `*.log` dan `Config.xml` diabaikan git.

## Pengembang

© 2026 Nur Fadilah Zulfi dan Diah Fika Satrya, untuk PT Tanimas Resources International.
