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
        if (!ModelState.IsValid) return Json(new { success = false, message = "Data is invalid." });
        try
        {
            await _userService.CreateUserAsync(userDto);
            return Json(new { success = true, message = "User successfully created!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }


    // Aksi untuk memproses edit user
    [HttpPost]
    public async Task<IActionResult> EditUser(UserDTO userDto)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Data is invalid." });
        try
        {
            await _userService.UpdateUserAsync(userDto);
            return Json(new { success = true, message = "User successfully updated!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
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
        if (!ModelState.IsValid) return Json(new { success = false, message = "Data is incomplete." });
        try
        {
            await _supplierService.CreateSupplierAsync(supplierDto);
            return Json(new { success = true, message = "Supplier successfully created!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> EditSupplier(SupplierDTO supplierDto)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Data is incomplete." });
        try
        {
            await _supplierService.UpdateSupplierAsync(supplierDto);
            return Json(new { success = true, message = "Supplier successfully updated!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
