using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sourcelist.DTOs;
using sourcelist.Services;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")] 
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly ISourceListService _sourceListService; 

    public AdminController(IUserService userService, ISourceListService sourceListService)
    {
        _userService = userService;
        _sourceListService = sourceListService;
    }



    // Aksi untuk menampilkan halaman Manage User
    //[HttpGet]
    //public async Task<IActionResult> ManageUser(int page = 1, int pageSize = 10, string searchTerm = null)
    //{
    //    var result = await _userService.GetAllUsersPagedAsync(page, pageSize, searchTerm);
    //    ViewBag.Page = page;
    //    ViewBag.PageSize = pageSize;
    //    ViewBag.TotalRows = result.TotalRows;
    //    ViewBag.SearchTerm = searchTerm;
    //    return View(result);
    //}

    //// Aksi untuk memproses penambahan user baru
    //[HttpPost]
    //public async Task<IActionResult> CreateUser(UserDTO userDto)
    //{
    //    await _userService.CreateUserAsync(userDto);
    //    return RedirectToAction("ManageUser");
    //}

    //// Aksi untuk memproses edit user
    //[HttpPost]
    //public async Task<IActionResult> EditUser(UserDTO userDto)
    //{
    //    await _userService.UpdateUserAsync(userDto);
    //    return RedirectToAction("ManageUser");
    //}

    // tambhan manage supplier dibawah sini
}