using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sourcelist.DTOs;
using sourcelist.Services;
using System.Threading.Tasks;

//  hanya bisa diakses oleh Admin
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly ISourceListService _sourceListService;
    private readonly ISupplierService _supplierService;

    public AdminController(IUserService userService, ISourceListService sourceListService, ISupplierService supplierService)
    {
        _userService = userService;
        _sourceListService = sourceListService;
        _supplierService = supplierService;

    }

    // Aksi untuk menampilkan halaman Manage User
    [HttpGet]
    public async Task<IActionResult> ManageUser(int page = 1, int pageSize = 10, string searchTerm = null)
    {
        
        var result = await _userService.GetAllUsersPagedAsync(page, pageSize, searchTerm);

        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRows = result.TotalRows;
        ViewBag.SearchTerm = searchTerm;
        return View(result);
    }

    // Aksi untuk memproses penambahan user baru
    [HttpPost]
    public async Task<IActionResult> CreateUser(UserDTO userDto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _userService.CreateUserAsync(userDto);

                TempData["SuccessMessage"] = "User has been successfully created!";
                return RedirectToAction("ManageUser");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("ManageUser");
            }
        }

        TempData["ErrorMessage"] = "Data is invalid or incomplete.";
        return RedirectToAction("ManageUser");
    }

    // Aksi untuk memproses edit user
    [HttpPost]
    public async Task<IActionResult> EditUser(UserDTO userDto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _userService.UpdateUserAsync(userDto);

                TempData["SuccessMessage"] = "User has been successfully updated!";
                return RedirectToAction("ManageUser");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("ManageUser");
            }
        }

        TempData["ErrorMessage"] = "Data is invalid or incomplete..";
        return RedirectToAction("ManageUser");
    }

    [HttpGet]
    public async Task<IActionResult> ManageSupplier(int page = 1, int pageSize = 10, string searchTerm = null)
    {
        var result = await _supplierService.GetAllSuppliersPagedAsync(page, pageSize, searchTerm);
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRows = result.TotalRows;
        ViewBag.SearchTerm = searchTerm;
        return View(result); 
    }

    [HttpPost]
    public async Task<IActionResult> CreateSupplier(SupplierDTO supplierDto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _supplierService.CreateSupplierAsync(supplierDto);
                TempData["SuccessMessage"] = "Supplier has been successfully created!";
                return RedirectToAction("ManageSupplier");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("ManageSupplier");
            }
        }
    

      TempData["ErrorMessage"] = "Data is invalid or incomplete..";

        var errorMessages = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

        TempData["ErrorMessage"] = "Validation Error: " + errorMessages;

        return RedirectToAction("ManageSupplier");
    }

    [HttpPost]
    public async Task<IActionResult> EditSupplier(SupplierDTO supplierDto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _supplierService.UpdateSupplierAsync(supplierDto);
                TempData["SuccessMessage"] = "Supplier has been successfully updated!";
                return RedirectToAction("ManageSupplier");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("ManageSupplier");
            }
        }
        TempData["ErrorMessage"] = "Data is invalid or incomplete..";
        return RedirectToAction("ManageSupplier");
        }
    }

