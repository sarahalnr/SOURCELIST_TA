using iText.Forms;
using iText.Kernel.Pdf;
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
using iText.Forms;
using iText.Kernel.Pdf;


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

            // jika data tidak ditemukan
            if (data == null)
            {
                return NotFound($"Source List with number {id} not found.");
            }
            // TENTUKAN LOKASI TEMPLATE
            string templatePath = Path.Combine(_webHostEnvironment.ContentRootPath,
                                               "Reports",
                                               "sourcelist template.pdf");

            MemoryStream outputStream = new MemoryStream();

            // BUKA, ISI, DAN KUNCI PDF
            using (PdfReader reader = new PdfReader(templatePath))
            using (PdfWriter writer = new PdfWriter(outputStream))
            using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
            {
                PdfAcroForm form = PdfAcroForm.GetAcroForm(pdfDoc, true);

                // ISI DATA
                form.GetField("Sourcelist No Field")?.SetValue(data.SourceListNumber);
                form.GetField("Requestor Field")?.SetValue(data.Requestor);
                form.GetField("Bau No Field")?.SetValue(data.BAUNumber);
                form.GetField("Part Description Field")?.SetValue(data.PartDescription);
                form.GetField("Supplier Name Field")?.SetValue(data.SupplierName);
                form.GetField("vendor Kode Field")?.SetValue(data.VendorCode);
                form.GetField("Supplier Status Field")?.SetValue(data.SupplierStatus);
                form.GetField("Sourcelist Status Field")?.SetValue(data.SourceListStatus);
                form.GetField("CMS# of final CRB field")?.SetValue(data.CMSFinalCRB); 
                form.GetField("Reason Field")?.SetValue(data.ReasonSubmission); 

                // Isi data Approver
       
                form.GetField("Approver Name field")?.SetValue(data.ApproverName);
                form.GetField("Approver Email Field")?.SetValue(data.ApproverEmail);
                form.GetField("Approver Status Field")?.SetValue(data.ApproverStatus);
                form.GetField("Validty Period field")?.SetValue(data.ValidityPeriod); 
                form.GetField("Remarks Field")?.SetValue(data.Remarks); 

                string tanggal = DateTime.Now.ToString("dd-MMM-yyyy");
                form.GetField("Date Field")?.SetValue(tanggal);

                // Kunci form agar tidak bisa diedit
                form.FlattenFields();
            }
            outputStream.Position = 0;
            string fileName = $"{id}.pdf";

            return File(outputStream, "application/pdf", fileName);
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
    