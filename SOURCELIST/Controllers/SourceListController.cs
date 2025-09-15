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

            //model.RequestorEmail = UserInfo.Email ;
          

            if (ModelState.IsValid)
            {
                try // untuk error handling
                {
                    string tempFileName = null; 

                    // Simpan file ke folder 
                    if (model.AttachmentFile != null)
                    {
                        
                        string tempFolder = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", "temp");
                        if (!Directory.Exists(tempFolder))
                        {
                            Directory.CreateDirectory(tempFolder);
                        }

                        tempFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AttachmentFile.FileName);
                        string tempFilePath = Path.Combine(tempFolder, tempFileName);

                     
                        using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                        {
                            await model.AttachmentFile.CopyToAsync(fileStream);
                        }
                    }

                  
                    string newSourceListId = await _sourceListService.CreateNewSourceListAsync(model, tempFileName);

                  
                    if (tempFileName != null)
                    {
                        string tempFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", "temp", tempFileName);

                        
                        if (System.IO.File.Exists(tempFilePath))
                        {
                            // path folder 
                            string finalFolder = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", newSourceListId);
                            if (!Directory.Exists(finalFolder))
                            {
                                Directory.CreateDirectory(finalFolder);
                            }

                            
                            string finalFileName = Path.GetFileName(model.AttachmentFile.FileName);
                            string finalFilePath = Path.Combine(finalFolder, finalFileName);

                         
                            System.IO.File.Move(tempFilePath, finalFilePath);

                         
                            string finalRelativePath = Path.Combine("attachments" , newSourceListId, finalFileName).Replace('\\', '/');

                        
                            await _sourceListService.UpdateAttachmentPathAsync(newSourceListId, finalRelativePath);
                        }
                    }
                    // Mengembalikan respons JSON untuk AJAX
                    return Ok(new { success = true, message = "Data berhasil disimpan!", newId = newSourceListId });
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