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

namespace sourcelist.Controllers
{
    public class SourceListController : Controller
    {
        private readonly ILDAPService _ldapService;
        private readonly ISourceListService _sourceListService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SourceListController(ILDAPService ldapService, ISourceListService sourceListService, IWebHostEnvironment webHostEnvironment)
        {
            _ldapService = ldapService;
            _sourceListService = sourceListService;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(SourceListCreateViewModel model)
        {
            var UserInfo = HttpContext.Session.GetObjectFromJson<sourcelist.Models.UserInfo>("UserInfo");

            if (model.SupplierStatus == "New" && model.AssessmentAttachmentFile == null)
            {
                // Jika status "New" tapi tidak ada file, tambahkan error 
                ModelState.AddModelError("AttachmentFile", "Supplier Assesment Form is required for new suppliers.");
            }

           
            if (model.SupplierStatus == "Transfer" && model.AttachedEndorsementFile == null)
            {
                ModelState.AddModelError("EndorsementFile", "Supplier Endorsement List is required for transfer suppliers.");
            }

            model.RequestorEmail = UserInfo.Email;

            
            if (!ModelState.IsValid)
            {
                // Mengembalikan error jika ada
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Data tidak valid: " + string.Join(", ", errors) });
            }

            try
            {
                string assessmentTempFileName = null;
                string endorsementTempFileName = null;
                string tempFolder = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", "temp");

           
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                // Simpan file Assessment ke folder temp
                if (model.AssessmentAttachmentFile != null)
                {
                    assessmentTempFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AssessmentAttachmentFile.FileName);
                    string tempFilePath = Path.Combine(tempFolder, assessmentTempFileName);
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await model.AssessmentAttachmentFile.CopyToAsync(fileStream);
                    }
                }

                // Simpan file Endorsement ke folder 
                if (model.AttachedEndorsementFile != null)
                {
                    endorsementTempFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AttachedEndorsementFile.FileName);
                    string tempFilePath = Path.Combine(tempFolder, endorsementTempFileName);
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await model.AttachedEndorsementFile.CopyToAsync(fileStream);
                    }
                }

                string newSourceListId = await _sourceListService.CreateNewSourceListAsync(model, assessmentTempFileName, endorsementTempFileName);

             
                string finalFolder = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", newSourceListId);
                if (!Directory.Exists(finalFolder))
                {
                    Directory.CreateDirectory(finalFolder);
                }

         
                if (assessmentTempFileName != null)
                {
                  
                    string tempFilePath = Path.Combine(tempFolder, assessmentTempFileName);
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        string finalFileName = Path.GetFileName(model.AssessmentAttachmentFile.FileName); // Nama file 
                        string finalFilePath = Path.Combine(finalFolder, finalFileName);
                        System.IO.File.Move(tempFilePath, finalFilePath);

                        string finalRelativePath = Path.Combine("attachments", newSourceListId, finalFileName).Replace('\\', '/');
                        await _sourceListService.UpdateAttachmentPathAsync(newSourceListId, finalRelativePath);
                    }
                }

             
                if (endorsementTempFileName != null)
                {
                    string tempFilePath = Path.Combine(tempFolder, endorsementTempFileName);
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        string finalFileName = Path.GetFileName(model.AttachedEndorsementFile.FileName);
                        string finalFilePath = Path.Combine(finalFolder, finalFileName);
                        System.IO.File.Move(tempFilePath, finalFilePath);

                        string finalRelativePath = Path.Combine("attachments", newSourceListId, finalFileName).Replace('\\', '/');
                        await _sourceListService.UpdateEndorsementPathAsync(newSourceListId, finalRelativePath);
                    }
                }

                return Ok(new { success = true, message = "Data berhasil disimpan!", newId = newSourceListId });
            }
            catch (Exception ex)
            {
             
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan server: " + ex.Message });
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
        public async Task<IActionResult> Detail(string id, string source)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Source List Number is required.");
            }

            var viewModel = await _sourceListService.GetSourceListDetailAsync(id);

            if (viewModel == null)
            {
                return NotFound($"Source List with number {id} not found.");
            }

            bool isFromApprovePage = "Approve".Equals(source, StringComparison.OrdinalIgnoreCase);

           
            bool isPending = "PENDING".Equals(viewModel.ApproverStatus, StringComparison.OrdinalIgnoreCase);
      

            ViewBag.ShowApprovalButtons = isFromApprovePage && isPending;

            if (isFromApprovePage)
            {
                ViewData["ReturnUrl"] = Url.Action("IndexForApprove", "SourceList");
            }
            else
            {
                ViewData["ReturnUrl"] = Url.Action("IndexMySourceList", "SourceList");
            }

            return View(viewModel);
        }


        [HttpGet]
        public JsonResult SearchApprovers(string term)
        {
            var users = _ldapService.GetAllUsers(term);
            var result = users.Select(u => new
            {
                id = u.UserName,
                text = $"{u.DisplayName}",
                email = u.Email,
                desc = u.BadgeNo
            });
            return Json(result);
        }
    }
}