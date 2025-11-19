using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models.CallRecords;
using Microsoft.VisualBasic;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using QuestPDF.Elements;
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


namespace sourcelist.Controllers
{
    public class SourceListController : Controller
    {
        private readonly ISourceListService _sourceListService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService; 

       
        public SourceListController(ISourceListService sourceListService,
                                    IWebHostEnvironment webHostEnvironment,
                                    ApplicationDbContext context,
                                    IEmailService emailService)
        {
            _sourceListService = sourceListService;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _emailService = emailService;
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
                return Unauthorized(new { success = false, message = "your sesion has expired. please log in again" });
            }
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var fullNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return StatusCode(500, new { success = false, message = "User ID information was not found in your login sesion" });
            }
            model.Requestor = fullNameClaim;
            model.RequestorEmail = emailClaim;
            model.RequestorId = int.Parse(userIdClaim);

      
            try
            {
                var approver = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Approver");
                if (approver == null)
                {
                    return StatusCode(500, new { success = false, message = "Sistem error: no user with the approver role found" });
                }
                model.ApproverId = approver.UserID;
                model.ApproverName = approver.Username;
                model.ApproverEmail = approver.Email;

                if (model.SupplierStatus == "New")
                {
                    if (string.IsNullOrWhiteSpace(model.SupplierName))
                    {
                        return BadRequest(new { success = false, message = "Supplier name is required for a new supplier." });
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
                        return BadRequest(new { success = false, message = $"Supplier ID '{model.SupplierId}' not found." });
                    }
                    model.SupplierName = supplier.NamaSupplier;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Gagal mengambil atau menyimpan data master: " + ex.Message });
            }

            if (model.SupplierStatus == "New" && model.AssessmentAttachmentFile == null)
            {
                ModelState.AddModelError("AssessmentAttachmentFile", "Supplier Assesment Form required for new supplier.");
            }
            if (model.SupplierStatus == "Transfer" && model.AttachedEndorsementFile == null)
            {
                ModelState.AddModelError("AttachedEndorsementFile", "Supplier Endorsement List required for supplier transfer.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Invalid data: " + string.Join(", ", errors) });
            }

            try
            {
                string assessmentFileName = (model.AssessmentAttachmentFile != null) ? Path.GetFileName(model.AssessmentAttachmentFile.FileName) : null;
                string endorsementFileName = (model.AttachedEndorsementFile != null) ? Path.GetFileName(model.AttachedEndorsementFile.FileName) : null;

                string newSourceListId = await _sourceListService.CreateNewSourceListAsync(model, assessmentFileName, endorsementFileName);

                if (string.IsNullOrEmpty(newSourceListId))
                {
                    return StatusCode(500, new { success = false, message = "Failed to create data, the stored procedure did not return in ID." });
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

                //  KIRIM EMAIL SUBMISSION (Mirip SP SOURCELIST_SEND_MAIL_SUBMISSION) 
                try
                {
                    string to = model.ApproverEmail;
                    string subject = $"New Source List Request for Approval: {newSourceListId}";
                    string linkUrl = $"{Request.Scheme}://{Request.Host}{Url.Action("IndexForApprove", "SourceList")}";

                   
                    string body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; color: #222e3c; font-size: 14px; }}
                            h3 {{ margin-bottom: 8px; color: #345E37; }}
                            table {{ border-collapse: collapse; margin-top: 10px; font-size: 14px; }}
                            td {{ padding: 6px 12px; border: 1px solid #e0e0e0; }}
                            .label {{ background: #A3D65C; color: #345E37; font-weight: bold; width: 180px; }}
                            .footer {{ font-size: 12px; color: #888; margin-top: 16px; }}
                            .button-container {{ margin-top: 20px; }}
                            .button {{
                                background-color: #345E37;
                                color: white;
                                padding: 10px 20px;
                                text-decoration: none;
                                border-radius: 5px;
                                font-weight: bold;
                                display: inline-block;
                            }}
                        </style>
                    </head>
                    <body>
                        <h3>New Source List Request for Approval</h3>
                        <p>Dear {model.ApproverName ?? "Approver"},</p>
                        <p>A new source list request has been submitted and requires your approval.</p>
                        <table>
                            <tr><td class='label'>Source List No.</td><td>{newSourceListId}</td></tr>
                            <tr><td class='label'>Requestor</td><td>{model.Requestor}</td></tr>
                            <tr><td class='label'>Part Number</td><td>{model.BAUNumber}</td></tr>
                            <tr><td class='label'>Part Description</td><td>{model.PartDescription}</td></tr>
                            <tr><td class='label'>Supplier</td><td>{model.SupplierName}</td></tr>
                            <tr><td class='label'>Reason of Submission</td><td>{model.ReasonSubmission}</td></tr>
                        </table>
                        <br/>
                        <p>Please check the details in the SOURCE LIST System Application.<br/> 
                        <div class='button-container'>
                            <a href='{linkUrl}' class='button'>Open Source List System</a>
                        </div>
                        
                        <br><p>Thank you,</p><p><b>Source List System</b></p>
                        <div class='footer'>This is an automatic email from <b>SOURCELIST System</b>. Do not reply to this email.</div> 
                    </body>
                    </html>";

                    // Panggil service email
                    await _emailService.SendEmailAsync(to, subject, body);
                }
                catch (Exception ex_email)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send email: {ex_email.Message}");
                   
                }

                return Ok(new { success = true, message = "Data has been succesfully saved!", newId = newSourceListId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while saving the data: " + (ex.InnerException?.Message ?? ex.Message) });
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
            // ... (kode Anda tidak berubah) ...
            if (!User.Identity.IsAuthenticated)
            {
                if (isAjax)
                {
                    return Unauthorized(new { message = "Session not found. Please log in." });
                }
                return RedirectToAction("Login", "Account");
            }

            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return StatusCode(500, "User email information not found in your session.");
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
                    return Unauthorized(new { message = "Session not found. Please log in." });
                }
                return RedirectToAction("Login", "Account");
            }
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return StatusCode(500, "User email information not found in your session.");
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

                // KIRIM EMAIL APPROVE (Mirip SP SOURCELIST_SEND_MAIL_APPROVE) ---
                try
                {
                    // Ambil detail data untuk dikirim ke email
                    var data = await _sourceListService.GetSourceListDetailAsync(model.SourceListNumber);
                    if (data != null)
                    {
                        string to = data.RequestorEmail;
                        string subject = $"[Source List] APPROVED - No: {model.SourceListNumber}";
                        string linkUrl = $"{Request.Scheme}://{Request.Host}{Url.Action("Detail", "SourceList", new { id = model.SourceListNumber })}";

                        string body = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; color: #222e3c; font-size: 14px; }}
                                h3 {{ margin-bottom: 8px; color: #155724; }}
                                .badge-approve {{ color: #fff; background: #28a745; border-radius: 4px; padding: 2px 10px; font-weight: bold; }}
                                table {{ border-collapse: collapse; margin-top: 10px; font-size: 14px; }}
                                td {{ padding: 6px 12px; border: 1px solid #e0e0e0; }}
                                .label {{ background: #A3D65C; color: #345E37; font-weight: bold; width: 180px; }}
                                .footer {{ font-size: 12px; color: #888; margin-top: 16px; }}
                                .button-container {{ margin-top: 20px; }}
                                .button {{
                                    background-color: #345E37;
                                    color: white;
                                    padding: 10px 20px;
                                    text-decoration: none;
                                    border-radius: 5px;
                                    font-weight: bold;
                                    display: inline-block;
                                }}
                            </style>
                        </head>
                        <body>
                            <h3>Source List Approved Notification</h3>
                            <p>Dear {data.Requestor},</p>
                            <p>The following source list has been <span class='badge-approve'>APPROVED</span> by <b>{data.ApproverName}</b>.</p>
                            <table>
                                <tr><td class='label'>Source List No.</td><td>{model.SourceListNumber}</td></tr>
                                <tr><td class='label'>Requestor</td><td>{data.Requestor}</td></tr>
                                <tr><td class='label'>Part Number</td><td>{data.BAUNumber}</td></tr>
                                <tr><td class='label'>Part Description</td><td>{data.PartDescription}</td></tr>
                                <tr><td class='label'>Supplier</td><td>{data.SupplierName}</td></tr>
                                <tr><td class='label'>Reason of Submission</td><td>{data.ReasonSubmission}</td></tr>
                                <tr><td class='label'>Approver Remark</td><td>{model.Remarks ?? "-"}</td></tr>
                            </table>
                            <br/>
                            <p>Please check the details in the Source List System Application.<br/> 
                            <div class='button-container'>
                                <a href='{linkUrl}' class='button'>Open Source List Detail</a>
                            </div>
                            <br><p>Thank you,</p><p><b>Source List System</b></p>
                            <div class='footer'>This is an automatic email from <b>SOURCELIST System</b>. Do not reply to this email.</div> 
                        </body>
                        </html>";

                        await _emailService.SendEmailAsync(to, subject, body);
                    }
                }
                catch (Exception ex_email)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send approval email: {ex_email.Message}");
                }
                // --- AKHIR KODE EMAIL ---

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

                // KIRIM EMAIL REJECT (Mirip SP SOURCELIST_SEND_MAIL_REJECT)
                try
                {
                    // Ambil detail data untuk dikirim ke email
                    var data = await _sourceListService.GetSourceListDetailAsync(model.SourceListNumber);
                    if (data != null)
                    {
                        string to = data.RequestorEmail;
                        string subject = $"[Source List] REJECTED - No: {model.SourceListNumber}";
                        string linkUrl = $"{Request.Scheme}://{Request.Host}{Url.Action("Detail", "SourceList", new { id = model.SourceListNumber })}";

                        string body = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; color: #222e3c; font-size: 14px; }}
                                h3 {{ margin-bottom: 8px; color: #a94442; }}
                                .badge-reject {{ color: #fff; background: #dc3545; border-radius: 4px; padding: 2px 10px; font-weight: bold; }}
                                table {{ border-collapse: collapse; margin-top: 10px; font-size: 14px; }}
                                td {{ padding: 6px 12px; border: 1px solid #e0e0e0; }}
                                .label {{ background: #A3D65C; color: #345E37; font-weight: bold; width: 180px; }}
                                .footer {{ font-size: 12px; color: #888; margin-top: 16px; }}
                                .button-container {{ margin-top: 20px; }}
                                .button {{
                                    background-color: #345E37;
                                    color: white;
                                    padding: 10px 20px;
                                    text-decoration: none;
                                    border-radius: 5px;
                                    font-weight: bold;
                                    display: inline-block;
                                }}
                            </style>S
                        </head>
                        <body>
                            <h3>Source List Rejected Notification</h3>
                            <p>Dear {data.Requestor},</p>
                            <p>The following source list has been <span class='badge-reject'>REJECTED</span> by <b>{data.ApproverName}</b>.</p>
                            <table>
                                <tr><td class='label'>Source List No.</td><td>{model.SourceListNumber}</td></tr>
                                <tr><td class='label'>Requestor</td><td>{data.Requestor}</td></tr>
                                <tr><td class='label'>Part Number</td><td>{data.BAUNumber}</td></tr>
                                <tr><td class='label'>Part Description</td><td>{data.PartDescription}</td></tr>
                                <tr><td class='label'>Supplier</td><td>{data.SupplierName}</td></tr>
                                <tr><td class='label'>Reason of Submission</td><td>{data.ReasonSubmission}</td></tr>
                                <tr><td class='label'>Reject Remark</td><td>{model.Remarks ?? "-"}</td></tr>
                            </table>
                            <br/>
                            <p>Please check the details and revise your submission in the Source List System Application.<br/> 
                            <div class='button-container'>
                                <a href='{linkUrl}' class='button'>Open Source List Detail</a>
                            </div>
                            <br><p>Thank you,</p><p><b>Source List System</b></p>
                            <div class='footer'>This is an automatic email from <b>SOURCELIST System</b>. Do not reply to this email.</div> 
                        </body>
                        </html>";

                        await _emailService.SendEmailAsync(to, subject, body);
                    }
                }
                catch (Exception ex_email)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send rejection email: {ex_email.Message}");
                }
                // --- AKHIR KODE EMAIL ---

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
                return BadRequest("File information is incomplete.");
            }

            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", id, fileName);


            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("File not found on the server.");
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
                    return Unauthorized(new { message = "Session not found. Please log in." });
                }
                return RedirectToAction("Login", "Account");
            }
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return StatusCode(500, "User email information not found in your session.");
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
        public async Task<IActionResult> ReportPdf(string id)
        {
            var data = await _sourceListService.GetSourceListDetailAsync(id);
            if (data == null)
            {
                return NotFound();
            }
            return View("ReportPdf", data);
        }


        [HttpGet]
        public async Task<IActionResult> DownloadSourceListPdf(string id)
        {
            string fileName = $"{id}.pdf";


            string reportUrl = $"{Request.Scheme}://{Request.Host}{Url.Action("ReportPdf", "SourceList", new { id = id })}";

            try
            {
                await new BrowserFetcher().DownloadAsync();

                await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox" }
                });
                await using var page = await browser.NewPageAsync();
                await page.GoToAsync(reportUrl, WaitUntilNavigation.Networkidle0);

                var pdfBytes = await page.PdfDataAsync(new PdfOptions
                {
                    Height = "140mm",
                    PrintBackground = true,
                    MarginOptions = new MarginOptions
                    {
                        Top = "40px",
                        Bottom = "40px",
                        Left = "40px",
                        Right = "40px"
                    }
                });
                await browser.CloseAsync();
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                // Tangani error 
                return StatusCode(500, $"Error generating PDF. please make sure you are the running application Error: {ex.Message}");
            }
        }
    }
}