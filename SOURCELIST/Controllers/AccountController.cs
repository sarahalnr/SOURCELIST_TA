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
    [ValidateAntiForgeryToken] // Untuk keamanan
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
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe, 
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddMinutes(30)
                };

              
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

              
                return RedirectToAction("Index", "Home");
            }
            else
            {
                
                ModelState.AddModelError(string.Empty, "Email atau Password salah.");
            }
        }

     
        return View(model);
    }



    public async Task<IActionResult> Logout()
    {
     
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
}