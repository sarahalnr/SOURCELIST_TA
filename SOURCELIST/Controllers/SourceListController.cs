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
            if (model.SupplierStatus == "New" && model.AttachmentFile == null)
            {
                // Jika status "New" tapi tidak ada file, tambahkan error 
                ModelState.AddModelError("AttachmentFile", "Supplier Assesment Form is required for new suppliers.");
            }

            model.RequestorEmail = UserInfo.Email ;
          

            if (ModelState.IsValid)
            {
                try // untuk error handling
                {
                    string uniqueFileName = null;
                    if (model.AttachmentFile != null)
                    {
                        // Path penyimpanan file 
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "attachments");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AttachmentFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.AttachmentFile.CopyToAsync(fileStream);
                        }
                    }

                    string newId = await _sourceListService.CreateNewSourceListAsync(model, uniqueFileName);

                    // Mengembalikan respons JSON untuk AJAX
                    return Ok(new { success = true, message = "Data berhasil disimpan!", newId = newId });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = "Terjadi kesalahan server: " + ex.Message });
                }
            }
            return BadRequest(new { success = false, message = "Data yang dikirim tidak valid." });
        }

        [HttpGet]
        public async Task<IActionResult> IndexMySourceList(int page = 1, int pageSize = 10, string searchTerm = null, bool isAjax = false)
        {
            
            var userInfo = HttpContext.Session.GetObjectFromJson<UserInfo>("UserInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Home");
            }

            string userFullName = userInfo.FullName;

            var result = await _sourceListService.GetSourceListsByEmailPagedAsync(userInfo.Email, userFullName, page, pageSize, searchTerm);
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRows = result.TotalRows;
            ViewBag.SearchTerm = searchTerm;

            if (isAjax)
            {
               
                return PartialView("_MySourceListTable", result); 
            }

            return View(result.Data); 
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