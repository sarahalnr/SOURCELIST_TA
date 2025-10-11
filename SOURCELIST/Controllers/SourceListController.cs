using sourcelist.Services;
using Microsoft.AspNetCore.Mvc;
using sourcelist.Models.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Linq;
using sourcelist.DTOs;
using sourcelist.Helper;
using sourcelist.Models;
using sourcelist.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        [ValidateAntiForgeryToken] // Tambahkan ini untuk keamanan
        public async Task<IActionResult> Create(SourceListCreateViewModel model)
        {
            // 1. PERIKSA APAKAH USER SUDAH LOGIN (CARA BARU YANG BENAR)
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { success = false, message = "Sesi Anda telah berakhir. Silakan login kembali." });
            }

            // 2. AMBIL DATA USER DARI CLAIMS ("KTP DIGITAL")
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var fullNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

            // Validasi jika karena suatu hal ID tidak ditemukan di dalam claims
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return StatusCode(500, new { success = false, message = "Informasi User ID tidak ditemukan di sesi login Anda." });
            }

            // 3. ISI DATA REQUESTOR DARI CLAIMS KE VIEWMODEL
            model.Requestor = fullNameClaim;
            model.RequestorEmail = emailClaim;
            model.RequestorId = int.Parse(userIdClaim);

            try
            {
                // 4. CARI APPROVER DAN SUPPLIER DARI DATABASE
                var approver = await _context.MUsers.FirstOrDefaultAsync(u => u.Role == "Approver");
                if (approver == null)
                {
                    return StatusCode(500, new { success = false, message = "Sistem error: Tidak ada user dengan role 'Approver' yang terdaftar." });
                }
                model.ApproverId = approver.UserID;

                var supplier = await _context.MSuppliers.FirstOrDefaultAsync(s => s.NamaSupplier == model.SupplierName);
                if (supplier == null)
                {
                    return BadRequest(new { success = false, message = $"Supplier dengan nama '{model.SupplierName}' tidak ditemukan di data master." });
                }
                model.SupplierId = supplier.ID_Supplier;
            }
            catch (Exception ex)
            {
                // Error ini biasanya terjadi jika ada masalah koneksi ke database
                return StatusCode(500, new { success = false, message = "Gagal terhubung ke database: " + ex.Message });
            }

            // 5. LAKUKAN VALIDASI FORM LAINNYA
            if (model.SupplierStatus == "New" && model.AssessmentAttachmentFile == null)
            {
                ModelState.AddModelError("AssessmentAttachmentFile", "Supplier Assesment Form wajib diisi untuk supplier baru.");
            }
            if (model.SupplierStatus == "Transfer" && model.AttachedEndorsementFile == null)
            {
                ModelState.AddModelError("AttachedEndorsementFile", "Supplier Endorsement List wajib diisi untuk supplier transfer.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Data tidak valid: " + string.Join(", ", errors) });
            }

            // 6. JIKA SEMUA VALID, PROSES FILE DAN PANGGIL SERVICE
            try
            {
                string assessmentFileName = (model.AssessmentAttachmentFile != null) ? Path.GetFileName(model.AssessmentAttachmentFile.FileName) : null;
                string endorsementFileName = (model.AttachedEndorsementFile != null) ? Path.GetFileName(model.AttachedEndorsementFile.FileName) : null;

                // Panggil service untuk menyimpan data ke database
                string newSourceListId = await _sourceListService.CreateNewSourceListAsync(model, assessmentFileName, endorsementFileName);

                // Jika ID tidak kembali, berarti ada error di SP
                if (string.IsNullOrEmpty(newSourceListId))
                {
                    return StatusCode(500, new { success = false, message = "Gagal membuat data di database. Stored Procedure tidak mengembalikan ID baru." });
                }

                // Buat folder final berdasarkan ID baru
                string finalFolder = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", newSourceListId);
                if (!Directory.Exists(finalFolder))
                {
                    Directory.CreateDirectory(finalFolder);
                }

                // Pindahkan file dari temp ke folder final
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
                // Menangkap error dari Service atau Stored Procedure
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan saat menyimpan data: " + ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> IndexMySourceList(
          int page = 1,
          int pageSize = 10,
          string sortColumn = "SubmitDate",
          string sortDirection = "DESC",
          string searchTerm = null,
          bool isAjax = false)
        {

            var UserInfo = HttpContext.Session.GetObjectFromJson<sourcelist.Models.UserInfo>("UserInfo");
            if (UserInfo == null)
            {
                TempData["SweetAlertMessage"] = "Session not found. You are redirecting to Home.";
                TempData["SweetAlertType"] = "error";
                TempData["SweetAlertRedirect"] = Url.Action("Index", "Home");
                return RedirectToAction("Index", "Home");
            }

            string email = UserInfo.Email;
            var result = await _sourceListService.GetSourceListsByEmailPagedAsync(email, page, pageSize, sortColumn, sortDirection, searchTerm);
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

            var UserInfo = HttpContext.Session.GetObjectFromJson<sourcelist.Models.UserInfo>("UserInfo");
            if (UserInfo == null)
            {
                TempData["SweetAlertMessage"] = "Session not found. You are redirecting to Home.";
                TempData["SweetAlertType"] = "error";
                TempData["SweetAlertRedirect"] = Url.Action("Index", "Home");
                return RedirectToAction("Index", "Home");
            }

            string email = UserInfo.Email;
            var result = await _sourceListService.GetSourceListsForApprovalPagedAsync(email, page, pageSize, sortColumn, sortDirection, searchTerm);
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
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Source List Number is required.");
            }

            var currentUser = HttpContext.Session.GetObjectFromJson<sourcelist.Models.UserInfo>("UserInfo");
            if (currentUser == null)
            {
                TempData["SweetAlertMessage"] = "Session not found. You are redirecting to Home.";
                TempData["SweetAlertType"] = "error";
                TempData["SweetAlertRedirect"] = Url.Action("Index", "Home");
                return RedirectToAction("Index", "Home");
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
            bool isCurrentUserTheApprover = currentUser.Email.Equals(viewModel.ApproverEmail, StringComparison.OrdinalIgnoreCase);

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
            var UserInfo = HttpContext.Session.GetObjectFromJson<sourcelist.Models.UserInfo>("UserInfo");
            if (UserInfo == null)
            {
                TempData["SweetAlertMessage"] = "Session not found. You are redirecting to Home.";
                TempData["SweetAlertType"] = "error";
                TempData["SweetAlertRedirect"] = Url.Action("Index", "Home");
                return RedirectToAction("Index", "Home");
            }

            string email = UserInfo.Email;

            // Memanggil semua data
            var result = await _sourceListService.GetSourceListsForAllSourceListPagedAsync(email, page, pageSize, sortColumn, sortDirection, searchTerm);

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
    