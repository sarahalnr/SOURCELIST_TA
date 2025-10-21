using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sourcelist.Data;
using sourcelist.DTOs;
using sourcelist.Helper;
using sourcelist.Models;
using sourcelist.Models.ViewModels;
using sourcelist.Services;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.IO;
namespace sourcelist.Controllers
{
    public class SourceListController : Controller
    {
        private readonly ISourceListService _sourceListService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context; 

        public SourceListController(ISourceListService sourceListService, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context)
        {
            _sourceListService = sourceListService;
            _webHostEnvironment = webHostEnvironment;
            _context = context; 
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SourceListCreateViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { success = false, message = "Sesi Anda telah berakhir. Silakan login kembali." });
            }
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var fullNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return StatusCode(500, new { success = false, message = "Informasi User ID tidak ditemukan di sesi login Anda." });
            }
            model.Requestor = fullNameClaim;
            model.RequestorEmail = emailClaim;
            model.RequestorId = int.Parse(userIdClaim);

            // Langkah validasi dan pengisian data 
            try
            {
                var approver = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Approver");
                if (approver == null)
                {
                    return StatusCode(500, new { success = false, message = "Sistem error: Tidak ada user dengan role 'Approver'." });
                }
                model.ApproverId = approver.UserID;
                model.ApproverName = approver.Username;
                model.ApproverEmail = approver.Email;

                if (model.SupplierStatus == "New")
                {
                    if (string.IsNullOrWhiteSpace(model.SupplierName))
                    {
                        return BadRequest(new { success = false, message = "Nama Supplier wajib diisi untuk supplier baru." });
                    }
                    var existingSupplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.NamaSupplier == model.SupplierName);

                    if (existingSupplier == null)
                    {
                        var newSupplier = new Supplier
                        {
                            NamaSupplier = model.SupplierName,
                            KodeVendor = model.VendorCode, 
                            EmailSupplier = "not.set@email.com", 
                            Status = "Aktif" 
                        };
                        _context.Suppliers.Add(newSupplier);
                        await _context.SaveChangesAsync();

         
                        model.SupplierId = newSupplier.ID_Supplier;
                    }
                    else
                    {
                        
                        model.SupplierId = existingSupplier.ID_Supplier;
                    }
                }
                else 
                {
                    var supplier = await _context.Suppliers.FindAsync(model.SupplierId);
                    if (supplier == null)
                    {
                        return BadRequest(new { success = false, message = $"Supplier dengan ID '{model.SupplierId}' tidak ditemukan." });
                    }
                    model.SupplierName = supplier.NamaSupplier;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Gagal mengambil atau menyimpan data master: " + ex.Message });
            }

            // Validasi file attachment
            if (model.SupplierStatus == "New" && model.AssessmentAttachmentFile == null)
            {
                ModelState.AddModelError("AssessmentAttachmentFile", "Supplier Assesment Form wajib diisi untuk supplier baru.");
            }
            if (model.SupplierStatus == "Transfer" && model.AttachedEndorsementFile == null)
            {
                ModelState.AddModelError("AttachedEndorsementFile", "Supplier Endorsement List wajib diisi untuk supplier transfer.");
            }

            // Cek ModelState SETELAH semua data model terisi
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Data tidak valid: " + string.Join(", ", errors) });
            }

            // Proses penyimpanan ke service 
            try
            {
                string assessmentFileName = (model.AssessmentAttachmentFile != null) ? Path.GetFileName(model.AssessmentAttachmentFile.FileName) : null;
                string endorsementFileName = (model.AttachedEndorsementFile != null) ? Path.GetFileName(model.AttachedEndorsementFile.FileName) : null;

                string newSourceListId = await _sourceListService.CreateNewSourceListAsync(model, assessmentFileName, endorsementFileName);

                if (string.IsNullOrEmpty(newSourceListId))
                {
                    return StatusCode(500, new { success = false, message = "Gagal membuat data. Stored Procedure tidak mengembalikan ID baru." });
                }

                string finalFolder = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", newSourceListId);
                if (!Directory.Exists(finalFolder))
                {
                    Directory.CreateDirectory(finalFolder);
                }

                if (model.AssessmentAttachmentFile != null)
                {
                    string finalFilePath = Path.Combine(finalFolder, assessmentFileName);
                    using (var fileStream = new FileStream(finalFilePath, FileMode.Create))
                    {
                        await model.AssessmentAttachmentFile.CopyToAsync(fileStream);
                    }
                }
                if (model.AttachedEndorsementFile != null)
                {
                    string finalFilePath = Path.Combine(finalFolder, endorsementFileName);
                    using (var fileStream = new FileStream(finalFilePath, FileMode.Create))
                    {
                        await model.AttachedEndorsementFile.CopyToAsync(fileStream);
                    }
                }

                return Ok(new { success = true, message = "Data berhasil disimpan!", newId = newSourceListId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan saat menyimpan data: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        public async Task<IActionResult> IndexMySourceList(
          int page = 1,
          int pageSize = 10,
          string sortColumn = "SubmitDate",
          string sortDirection = "DESC",
          string searchTerm = null,
          bool isAjax = false)
        {
            // untuk cek login
            if (!User.Identity.IsAuthenticated)
            {
                if (isAjax)
                {
                    return Unauthorized(new { message = "Sesi Anda telah berakhir. Silakan login kembali." });
                }
                return RedirectToAction("Login", "Account"); 
            }

            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return StatusCode(500, "Informasi email tidak ditemukan di sesi login Anda.");
            }

            var result = await _sourceListService.GetSourceListsByEmailPagedAsync(emailClaim, page, pageSize, sortColumn, sortDirection, searchTerm);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRows = result.TotalRows;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDirection = sortDirection;
            ViewBag.SearchTerm = searchTerm;

            if (isAjax)
            {
                return PartialView("_MySourceListTable", result);
            }

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> IndexForApprove(
    int page = 1,
    int pageSize = 10,
    string sortColumn = "SubmitDate",
    string sortDirection = "DESC",
    string searchTerm = null,
    bool isAjax = false)
        {
            if (!User.Identity.IsAuthenticated)
            {
                if (isAjax)
                {
                    return Unauthorized(new { message = "Sesi Anda telah berakhir. Silakan login kembali." });
                }
                return RedirectToAction("Login", "Account");
            }
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return StatusCode(500, "Informasi email tidak ditemukan di sesi login Anda.");
            }

            var result = await _sourceListService.GetSourceListsForApprovalPagedAsync(emailClaim, page, pageSize, sortColumn, sortDirection, searchTerm);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRows = result.TotalRows;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDirection = sortDirection;
            ViewBag.SearchTerm = searchTerm;
            ViewData["Source"] = "Approve";

            if (isAjax)
            {
                return PartialView("_MySourceListTable", result);
            }

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(string id, string source, int page = 1)
        {
            if (!User.Identity.IsAuthenticated)
            {
                
                TempData["SweetAlertMessage"] = "Session not found. Please log in.";
                TempData["SweetAlertType"] = "error";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Source List Number is required.");
            }
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return StatusCode(500, "User email information not found in your session.");
            }

            var viewModel = await _sourceListService.GetSourceListDetailAsync(id);

            if (viewModel == null)
            {
                return NotFound($"Source List with number {id} not found.");
            }

            bool isFromAllowedPage = "Approve".Equals(source, StringComparison.OrdinalIgnoreCase) ||
                                     "All".Equals(source, StringComparison.OrdinalIgnoreCase) ||
                                     "AllSourceList".Equals(source, StringComparison.OrdinalIgnoreCase);

            bool isPending = "PENDING".Equals(viewModel.ApproverStatus, StringComparison.OrdinalIgnoreCase);

          
            bool isCurrentUserTheApprover = emailClaim.Equals(viewModel.ApproverEmail, StringComparison.OrdinalIgnoreCase);

            ViewBag.ShowApprovalButtons = isFromAllowedPage && isPending && isCurrentUserTheApprover;

            if ("Approve".Equals(source, StringComparison.OrdinalIgnoreCase))
            {
                ViewData["ReturnUrl"] = Url.Action("IndexForApprove", "SourceList", new { page = page });
            }
            else if ("All".Equals(source, StringComparison.OrdinalIgnoreCase) || "AllSourceList".Equals(source, StringComparison.OrdinalIgnoreCase))
            {
                ViewData["ReturnUrl"] = Url.Action("AllSourceList", "SourceList", new { page = page });
            }
            else
            {
                ViewData["ReturnUrl"] = Url.Action("IndexMySourceList", "SourceList", new { page = page });
            }

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Approve([FromBody] ApprovalViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.SourceListNumber))
            {
                return BadRequest(new { success = false, message = "Invalid data." });
            }
            try
            {
                await _sourceListService.ApproveSourceListAsync(model);
                return Ok(new { success = true, message = "SourceList has been approved." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Reject([FromBody] ApprovalViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.SourceListNumber))
            {
                return BadRequest(new { success = false, message = "Invalid data." });
            }
            try
            {
                await _sourceListService.RejectSourceListAsync(model);
                return Ok(new { success = true, message = "SourceList has been rejected." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DownloadFile(string id, [FromQuery] string fileName)
        {

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Informasi file tidak lengkap.");
            }

            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", id, fileName);


            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("File tidak ditemukan di server.");
            }


            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, "application/octet-stream", fileName);
        }


        [HttpGet]
        public async Task<IActionResult> AllSourceList(
    int page = 1,
    int pageSize = 10,
    string sortColumn = "SubmitDate",
    string sortDirection = "DESC",
    string searchTerm = null,
    bool isAjax = false)
        {
            if (!User.Identity.IsAuthenticated)
            {
                if (isAjax)
                {
                    return Unauthorized(new { message = "Sesi Anda telah berakhir. Silakan login kembali." });
                }
                return RedirectToAction("Login", "Account");
            }
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return StatusCode(500, "Informasi email tidak ditemukan di sesi login Anda.");
            }

            var result = await _sourceListService.GetSourceListsForAllSourceListPagedAsync(emailClaim, page, pageSize, sortColumn, sortDirection, searchTerm);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRows = result.TotalRows;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDirection = sortDirection;
            ViewBag.SearchTerm = searchTerm;
            ViewData["Source"] = "AllSourceList";

            if (isAjax)
            {
                return PartialView("_MySourceListTable", result);
            }

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers(string term) 
        {
            try
            {
                var query = _context.Suppliers.Where(s => s.Status == "Aktif");

                if (!string.IsNullOrEmpty(term))
                {
                    query = query.Where(s => s.NamaSupplier.Contains(term));
                }

                var suppliers = await query
                    .Select(s => new {
                        id = s.ID_Supplier,
                        text = s.NamaSupplier
                    })
                    .ToListAsync();

                return Json(suppliers);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetSuppliers: {ex.Message}");
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSupplierDetail(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id); 
            if (supplier == null)
            {
                return Json(null);
            }
            return Json(new { kodeVendor = supplier.KodeVendor });
        }
        [HttpGet]
        public async Task<IActionResult> DownloadSourceListPdf(string id)
        {
            var data = await _sourceListService.GetSourceListDetailAsync(id);

            if (data == null)
            {
                return NotFound($"Source List with number {id} not found.");
            }

            string templatePath = Path.Combine(_webHostEnvironment.ContentRootPath,
                                                "Reports",
                                                "sourcelist template.pdf");

            byte[] pdfBytes;
            string fileName = $"{id}.pdf";
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (PdfDocument document = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify))
                {
                    if (document.AcroForm != null)
                    {
                        PdfAcroForm form = document.AcroForm;

                        System.Diagnostics.Debug.WriteLine("===== NAMA-NAMA FIELD (PdfSharp) =====");
                        foreach (string fieldName in form.Fields.Names)
                        {
                            System.Diagnostics.Debug.WriteLine($"Nama Internal Field: {fieldName}");
                        }
                        System.Diagnostics.Debug.WriteLine("========================================");

                        (form.Fields["text_1elwr"] as PdfTextField).Text = data.SourceListNumber ?? "";
                        (form.Fields["text_2ztzh"] as PdfTextField).Text = data.Requestor ?? "";
                        (form.Fields["text_3iovz"] as PdfTextField).Text = data.BAUNumber ?? "";
                        (form.Fields["text_4qahh"] as PdfTextField).Text = data.PartDescription ?? "";
                        (form.Fields["text_5kfoc"] as PdfTextField).Text = data.SupplierName ?? "";
                        (form.Fields["text_6zqby"] as PdfTextField).Text = data.VendorCode ?? "";
                        (form.Fields["text_7odpb"] as PdfTextField).Text = data.SupplierStatus ?? "";
                        (form.Fields["text_8dnui"] as PdfTextField).Text = data.SourceListStatus ?? "";
                        (form.Fields["text_9ogqw"] as PdfTextField).Text = data.CMSFinalCRB ?? "";
                        (form.Fields["text_10ktwi"] as PdfTextField).Text = data.ReasonSubmission ?? "";

                        (form.Fields["text_11odkl"] as PdfTextField).Text = data.ApproverName ?? "";
                        (form.Fields["text_12ahjh"] as PdfTextField).Text = data.ApproverEmail ?? "";
                        (form.Fields["text_13gfkz"] as PdfTextField).Text = data.ApproverStatus ?? "";
                        (form.Fields["text_16geno"] as PdfTextField).Text = data.ValidityPeriod ?? "";
                        (form.Fields["text_18guia"] as PdfTextField).Text = data.Remarks ?? "";

                        string tanggal = DateTime.Now.ToString("dd-MMM-yyyy");
                        (form.Fields["text_19drmf"] as PdfTextField).Text = tanggal;

                        foreach (string fieldName in form.Fields.Names)
                        {
                            // Ambil field-nya berdasarkan nama, LALU set ReadOnly
                            PdfAcroField field = form.Fields[fieldName];
                            if (field != null)
                            {
                                field.ReadOnly = true;
                            }
                        }
                    }

                    document.Save(outputStream);
                } 
                pdfBytes = outputStream.ToArray();
            } 
            return File(pdfBytes, "application/pdf", fileName);
        }

        //[HttpGet]
        //public JsonResult SearchApprovers(string term)
        //{
        //    var users = _ldapService.GetAllUsers(term);
        //    var result = users.Select(u => new
        //    {
        //        id = u.UserName,
        //        text = $"{u.DisplayName}",
        //        email = u.Email,
        //        desc = u.BadgeNo
        //    });
        //    return Json(result);
        //}


    }
}
    