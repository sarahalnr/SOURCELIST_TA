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
        await _userService.CreateUserAsync(userDto);
        return RedirectToAction("ManageUser");
    }

    // Aksi untuk memproses edit user
    [HttpPost]
    public async Task<IActionResult> EditUser(UserDTO userDto)
    {
        await _userService.UpdateUserAsync(userDto);
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
        await _supplierService.CreateSupplierAsync(supplierDto);
        return RedirectToAction("ManageSupplier");
    }

    [HttpPost]
    public async Task<IActionResult> EditSupplier(SupplierDTO supplierDto)
    {
        await _supplierService.UpdateSupplierAsync(supplierDto);
        return RedirectToAction("ManageSupplier");
    }
}
