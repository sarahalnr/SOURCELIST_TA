using Microsoft.AspNetCore.Mvc;
using sourcelist.Services;
using sourcelist.Models.ViewModels;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;

public class AccountController : Controller
{
    private readonly IUserService _userService;

   
    public AccountController(IUserService userService)
    {
        _userService = userService;
    }

   
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userService.AuthenticateAsync(model.Email, model.Password);

            if (user != null)
            {
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID_User.ToString()),
                new Claim(ClaimTypes.Name, user.Username.Trim()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Trim())
            };

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ClaimTypes.Name,
                    ClaimTypes.Role);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                //TempData["LoginStatus"] = "Success";
                TempData["SweetAlertType"] = "success";
                TempData["SweetAlertMessage"] = "Login Success!";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["LoginStatus"] = "Error";
                ModelState.AddModelError(string.Empty, "The email or password you entered is incorrect.");
            }
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View("ChangePass");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("ChangePass", model);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return RedirectToAction("Login"); 
        }

        int userId = int.Parse(userIdString);
        bool isSuccess = await _userService.ChangePasswordAsync(userId, model.OldPassword, model.NewPassword);

        if (isSuccess)
        {
            TempData["SweetAlertType"] = "success";
            TempData["SweetAlertMessage"] = "Password successfully updated!";
            return RedirectToAction("ChangePassword", "Account");
        }
        else
        {
            // Jika gagal (karena password salah)
            TempData["SweetAlertType"] = "error";
            TempData["SweetAlertMessage"] = "Failed to change password. Check your old password.";
            ModelState.AddModelError("OldPassword", "Password lama tidak sesuai.");
            return View("ChangePass", model);
        }
    }

    public async Task<IActionResult> Logout()
        {
     
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
}
