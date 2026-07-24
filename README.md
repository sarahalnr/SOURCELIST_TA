# 📦 SourceList — Web-Based Supplier Approval System

> **Sistem Informasi Persetujuan SourceList Berbasis Web di PT XYZ Electronics**  
> *Laporan Tugas Akhir — Program Studi D3 Teknik Informatika, Politeknik Negeri Batam*

---

## 📌 Deskripsi Proyek

**SourceList System** adalah aplikasi berbasis web yang dirancang untuk mengotomatisasi, mendokumentasikan, dan mempercepat alur kerja pendaftaran serta persetujuan (*SourceList Creation*) supplier baru maupun perpanjangan secara digital. 

Sebelum adanya sistem ini, proses pengajuan dilakukan secara manual berbasis email antar tim *Buyer* dan *Supplier Quality Engineering (SQE)*. Hal ini mengakibatkan inefisiensi, lambatnya proses kualifikasi, serta kesukaran dalam melakukan pelacakan status (*tracking*) dan penarikan data historis untuk kebutuhan audit. Aplikasi ini hadir sebagai solusi digitalisasi terpusat untuk meningkatkan transparansi, akurasi pencatatan data, serta efisiensi operasional.

---

## 🎯 Tujuan & Manfaat

- **Workflow Automation:** Mengubah alur persetujuan fisik/email manual menjadi alur kerja digital yang terpusat dari *Submit* hingga keputusan final *Approver*.
- **Real-Time Tracking & Transparency:** Menyediakan pemantauan posisi dokumen secara *real-time* untuk mencegah penumpukan antrean pengajuan.
- **Digital Reporting & Audit Trail:** Menghasilkan rekapitulasi laporan berformat PDF untuk kebutuhan rekap data dan pemeriksaan kepatuhan (*compliance*).
- **Dukungan SDGs:** Mendukung **SDG Goal 9 (Industry, Innovation, and Infrastructure)** melalui inovasi administrasi digital yang efisien dan berkelanjutan di sektor manufaktur.

---

## ✨ Fitur Utama

### 🌐 System Features

| Fitur | Deskripsi |
| :--- | :--- |
| **Authentication & RBAC** | Login terautentikasi sesuai hak akses peran (*Requestor*, *Approver*, dan *Admin*). |
| **New SourceList Submission** | Formulir digital untuk mengunggah dokumen kualifikasi PDF (*Supplier Assessment Form*, *CRB Approval*, dll). |
| **Approval Management** | Fitur khusus *Approver* (SQE) untuk mengevaluasi pengajuan dengan input *Validity Period* dan *Remarks*. |
| **My SourceList & History** | Pemantauan riwayat pengajuan mandiri berbasis *Requestor*. |
| **All SourceList Monitoring** | Pusat data monitoring rekapitulasi pengajuan dari seluruh departemen. |
| **PDF Report Generation** | Pencetakan/pengunduhan otomatis berkas laporan akhir PDF untuk form yang telah disetujui. |
| **Automated Email Notification** | Pengiriman surel otomatis via SMTP kepada pihak terkait saat ada pengajuan baru atau perubahan status. |
| **Master Data Management** | Pengelolaan data *User* dan *Supplier* oleh Admin untuk menjaga validitas data master. |

---

## 🛠️ Tech Stack & Methodology

### Architecture & Tech Stack
* **Framework:** ASP.NET Core MVC (.NET 8)
* **Language:** C#
* **Database Management:** Microsoft SQL Server
* **Authentication:** Cookie-based Auth & LDAP Support
* **Frontend UI:** Razor Views (`.cshtml`), HTML5, CSS3, JavaScript, jQuery, Bootstrap
* **Development Methodology:** Waterfall Model
* **Software Testing:** Blackbox Testing (Equivalence Partitioning & Decision Table Testing) via UAT

---

## 📂 Struktur Proyek

```text
SOURCELIST/
├── Controllers/            # Handling HTTP requests & business logic
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── HomeController.cs
│   └── SourceListController.cs
├── Data/                   # Entity Framework ApplicationDbContext
├── DTOs/                   # Data Transfer Objects
├── Infrastructure/         # Core Contracts & Interfaces
├── Models/                 # Database Models & ViewModels
│   ├── ViewModels/         # Approval, Login, ChangePassword, Chart ViewModels
│   ├── Sourcelist.cs
│   ├── SourcelistDetail.cs
│   ├── Supplier.cs
│   └── User.cs
├── Services/               # Business Logic Implementations
│   ├── EmailService.cs
│   ├── LDAPService.cs
│   ├── SourceListService.cs
│   ├── SupplierService.cs
│   └── UserService.cs
├── Views/                  # Razor View Templates
│   ├── Account/
│   ├── Admin/
│   ├── Home/
│   ├── Shared/
│   └── SourceList/
├── wwwroot/                # Static Assets (CSS, JS, Custom Plugins, Images)
├── appsettings.json        # Environment Configuration & Connection Strings
└── Program.cs              # Application Entry Point & Service Dependency Injection
```

---

## 🗃️ Database Schema (ERD)

<img width="100%" alt="ERD REVISI drawio" src="https://github.com/user-attachments/assets/c4a4aaca-0ef0-48bf-9e71-4d8d3ae73bac" />

<details>
<summary>🔍 <b>Klik untuk melihat rincian Struktur Tabel & Tipe Data</b></summary>

### 1. `M_USER` (Master User)
* **`UserID`** (`int`, PK, Auto-Increment) — ID unik pengguna.
* **`Username`** (`nvarchar(50)`, Unique, Not Null) — Nama akun pengguna.
* **`UserPassword`** (`nvarchar(255)`, Not Null) — Hashing kata sandi (BCrypt).
* **`Email`** (`nvarchar(100)`, Unique, Not Null) — Surel resmi pengguna.
* **`Role`** (`nvarchar(20)`, Not Null) — Hak akses pengguna (`Admin`, `Requestor`, `Approver`).
* **`Status`** (`nvarchar(15)`, Not Null) — Status keaktifan akun (`Active` / `Inactive`).
* **`CreatedAt`** (`datetime`, Nullable) — Waktu akun dibuat.

---

### 2. `M_SUPPLIER` (Master Supplier)
* **`ID_Supplier`** (`int`, PK, Auto-Increment) — ID unik supplier.
* **`NamaSupplier`** (`nvarchar(100)`, Not Null) — Nama perusahaan supplier.
* **`KodeVendor`** (`nvarchar(50)`, Unique, Not Null) — Kode unik vendor/supplier.
* **`EmailSupplier`** (`nvarchar(100)`, Nullable) — Surel utama supplier.
* **`PICName`** (`nvarchar(100)`, Nullable) — Nama penanggung jawab (*Person in Charge*).
* **`PICEmail`** (`nvarchar(100)`, Nullable) — Surel penanggung jawab.
* **`Status`** (`nvarchar(15)`, Not Null) — Status keaktifan supplier.
* **`CreatedAt`** (`datetime`, Nullable) — Waktu data dibuat.

---

### 3. `T_SOURCELIST` (Transaksi Utama)
* **`SourceListNumber`** (`nvarchar(25)`, PK, Not Null) — Nomor dokumen persetujuan.
* **`ID_Requestor`** (`int`, FK ➔ `M_USER.UserID`) — Pengaju dokumen.
* **`ID_Approver`** (`int`, FK ➔ `M_USER.UserID`) — Penyetujui dokumen (SQE).
* **`ID_Supplier`** (`int`, FK ➔ `M_SUPPLIER.ID_Supplier`) — Supplier terkait.
* **`BAUNumber`** (`nvarchar(100)`, Nullable) — Nomor BAU/Plant.
* **`PartDescription`** (`nvarchar(255)`, Nullable) — Deskripsi komponen/material.
* **`SupplierStatus`** (`nvarchar(30)`, Nullable) — Status supplier (`New` / `Transfer`).
* **`SourceListStatus`** (`nvarchar(50)`, Nullable) — Status pengajuan.
* **`SubmittedDate`** (`datetime`, Not Null) — Waktu dokumen diajukan.
* **`ApprovalStatus`** (`nvarchar(20)`, Not Null) — Status persetujuan (`Pending`, `Approved`, `Rejected`).
* **`ApproveDate`** (`datetime`, Nullable) — Waktu dokumen disetujui/ditolak.

---

### 4. `T_SOURCELIST_DETAIL` (Detail Pelengkap)
* **`SourceListNumber`** (`nvarchar(25)`, PK & FK ➔ `T_SOURCELIST.SourceListNumber`) — Relasi *One-to-One*.
* **`ReasonSubmission`** (`nvarchar(max)`, Nullable) — Alasan pengajuan.
* **`CMSFinalCRB`** (`nvarchar(100)`, Nullable) — Nomor/Persetujuan CRB Final.
* **`AttachmentFileName`** (`nvarchar(100)`, Nullable) — Nama file lampiran PDF.
* **`AttachedEndorsement`** (`nvarchar(100)`, Nullable) — File/List Endorsement.
* **`Remarks`** (`nvarchar(max)`, Nullable) — Catatan/justifikasi dari Approver.
* **`ValidityPeriod`** (`nvarchar(20)`, Nullable) — Masa berlaku persetujuan (`Temporary` / `Permanent`).

</details>

---

## 🚀 Instalasi & Setup Lokal

### Prasyarat System
Pastikan perangkat Anda telah terinstal:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [SQL Server Management Studio (SSMS)](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)
- Git

### Langkah Pengaturan

1. **Clone Repository**
   ```bash
   git clone https://github.com/sarahalnr/SOURCELIST_TA.git
   cd SOURCELIST_TA
   ```

2. **Konfigurasi Database & Connection String**
   - Buat database `dbTA` pada Microsoft SQL Server.
   - Buka berkas `appsettings.json` dan sesuaikan koneksi database Anda:
     ```json
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=YOUR_SERVER_NAME;Initial Catalog=dbTA;Integrated Security=True;TrustServerCertificate=True;"
     }
     ```

3. **Jalankan Migrasi & Update Database**
   ```bash
   dotnet ef database update
   ```

4. **Jalankan Aplikasi**
   ```bash
   dotnet run
   ```

5. **Akses Aplikasi**
   Akses via peramban di `http://localhost:5000` atau `https://localhost:7001`.

---

## 👩‍💻 Pengembang

- **Nama:** Sarah Isnaini Alnauri
- **NIM:** 3312301018
- **Program Studi:** D3 Teknik Informatika — Politeknik Negeri Batam

---

## 📄 Lisensi & Hak Cipta

Project ini dikembangkan sebagai bagian dari **Tugas Akhir Akademik** di **Politeknik Negeri Batam** bekerja sama dengan **PT XYZ Electronics**. Seluruh data pengujian dalam repositori ini menggunakan data simulasi/artifisial (*dummy data*).
