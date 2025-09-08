using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using sourcelist.Models;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Runtime.Versioning;
using sourcelist.Models.ViewModels;
using UserInfo = sourcelist.Models.UserInfo;
using sourcelist.Helper;
namespace sourcelist.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public UserInfo CurrentUserInfo { get; set; }

        [SupportedOSPlatform("windows")]
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
        }

        private UserInfo? GetCurrentUserInfo()
        {
            return HttpContext.Session.GetObjectFromJson<UserInfo>("UserInfo");
        }

        public async Task<ActionResult> Index(string returnurl = null)
        {
            // Cek session
            var userInfo = HttpContext.Session.GetObjectFromJson<UserInfo>("UserInfo");
            if (userInfo == null)
            {
                var getLoggedInUser = User.Identity?.Name;
                if (!string.IsNullOrEmpty(getLoggedInUser) && getLoggedInUser.Contains("\\"))
                {
                    string[] arr = getLoggedInUser.Split('\\');
                    if (arr.Length == 2)
                    {
                        string domain = arr[0];
                        string username = arr[1];
                        using (var ldapContext = new PrincipalContext(ContextType.Domain, domain))
                        {
                            UserPrincipal user = UserPrincipal.FindByIdentity(ldapContext, username);
                            if (user != null)
                            {
                                userInfo = new UserInfo
                                {
                                    FullName = user.DisplayName ?? "No Display Name",
                                    Email = user.EmailAddress ?? "No Email",
                                    IDCard = user.Description ?? "No ID Card"
                                };

                                // Ambil role dari database
                                //var (roleId, roleName, isAssigner, isOpex) = await _userService.GetUserRoleByBadgeNoAsync(userInfo.IDCard);
                                //userInfo.RoleID = roleId?.ToString();
                                //userInfo.RoleName = roleName;
                                //userInfo.isAssigner = isAssigner ?? false;
                                //userInfo.isOpex = isOpex  ?? false;

                                // Ambil supervisor, lalu set ke userInfo
                                var supervisor = await GetSupervisorInfoUsingUserPrincipal(domain, user);
                                if (supervisor != null)
                                {
                                    userInfo.Supervisor = supervisor;
                                }

                                // Simpan ke session
                                HttpContext.Session.SetObjectAsJson("UserInfo", userInfo);
                            }
                        }
                    }
                }
            }
            //// === bagian untuk jumper email ===
            if (!string.IsNullOrEmpty(returnurl))
                return Redirect(returnurl);

            if (userInfo == null)
                return RedirectToAction("Index", "Home"); 


            ViewBag.UserInfo = userInfo;
            ViewData["Title"] = "Home";
            return View();
        }

        //public async Task<ActionResult> Index(string returnurl = null)
        //{
        //    var userInfo = HttpContext.Session.GetObjectFromJson<UserInfo>("UserInfo");

        //    // Jika session belum ada, simpan returnurl ke TempData, lalu proses login dan redirect ke Index
        //    if (userInfo == null)
        //    {
        //        if (!string.IsNullOrEmpty(returnurl))
        //            TempData["returnurl"] = returnurl;

        //        var getLoggedInUser = User.Identity?.Name;
        //        if (!string.IsNullOrEmpty(getLoggedInUser) && getLoggedInUser.Contains("\\"))
        //        {
        //            string[] arr = getLoggedInUser.Split('\\');
        //            if (arr.Length == 2)
        //            {
        //                string domain = arr[0];
        //                string username = arr[1];
        //                using (var ldapContext = new PrincipalContext(ContextType.Domain, domain))
        //                {
        //                    UserPrincipal user = UserPrincipal.FindByIdentity(ldapContext, username);
        //                    if (user != null)
        //                    {
        //                        userInfo = new UserInfo
        //                        {
        //                            FullName = user.DisplayName ?? "No Display Name",
        //                            Email = user.EmailAddress ?? "No Email",
        //                            IDCard = user.Description ?? "No ID Card"
        //                        };

        //                        var supervisor = await GetSupervisorInfoUsingUserPrincipal(domain, user);
        //                        if (supervisor != null)
        //                        {
        //                            userInfo.Supervisor = supervisor;
        //                        }

        //                        HttpContext.Session.SetObjectAsJson("UserInfo", userInfo);
        //                    }
        //                }
        //            }
        //        }

        //        // Setelah session di-set, redirect ke Index tanpa returnurl (agar tidak infinite loop)
        //        return RedirectToAction("Index", "Home");
        //    }

        //    // Jika session sudah ada, cek TempData untuk returnurl dan redirect ke detail
        //    if (TempData["returnurl"] != null)
        //    {
        //        string returnurlTemp = TempData["returnurl"].ToString();
        //        string decodedReturnUrl = HttpUtility.UrlDecode(returnurlTemp);

        //        if (!string.IsNullOrEmpty(decodedReturnUrl) && decodedReturnUrl.StartsWith("http://btmprd01"))
        //        {
        //            Response.Redirect(decodedReturnUrl, false);
        //            return new EmptyResult();
        //        }
        //    }

        //    // Jika tidak ada returnurl, tampilkan Home biasa
        //    ViewBag.UserInfo = userInfo;
        //    ViewData["Title"] = "Home";
        //    return View();
        //}


        //public async Task<ActionResult> Index(string returnurl = null)
        //{
        //    var userInfo = HttpContext.Session.GetObjectFromJson<UserInfo>("UserInfo");

        //    // Jika session belum ada, coba set session, tapi cegah infinite loop
        //    if (userInfo == null)
        //    {
        //        // Cegah infinite redirect: hanya boleh sekali mencoba set session
        //        if (TempData["TriedSetSession"] == null)
        //        {
        //            if (!string.IsNullOrEmpty(returnurl))
        //                TempData["returnurl"] = returnurl;

        //            TempData["TriedSetSession"] = "1";

        //            // Proses login/LDAP seperti biasa
        //            var getLoggedInUser = User.Identity?.Name;
        //            if (!string.IsNullOrEmpty(getLoggedInUser) && getLoggedInUser.Contains("\\"))
        //            {
        //                string[] arr = getLoggedInUser.Split('\\');
        //                if (arr.Length == 2)
        //                {
        //                    string domain = arr[0];
        //                    string username = arr[1];
        //                    using (var ldapContext = new PrincipalContext(ContextType.Domain, domain))
        //                    {
        //                        UserPrincipal user = UserPrincipal.FindByIdentity(ldapContext, username);
        //                        if (user != null)
        //                        {
        //                            userInfo = new UserInfo
        //                            {
        //                                FullName = user.DisplayName ?? "No Display Name",
        //                                Email = user.EmailAddress ?? "No Email",
        //                                IDCard = user.Description ?? "No ID Card"
        //                            };

        //                            var supervisor = await GetSupervisorInfoUsingUserPrincipal(domain, user);
        //                            if (supervisor != null)
        //                            {
        //                                userInfo.Supervisor = supervisor;
        //                            }

        //                            HttpContext.Session.SetObjectAsJson("UserInfo", userInfo);
        //                        }
        //                    }
        //                }
        //            }

        //            // Redirect ke Index lagi agar session ter-load
        //            return RedirectToAction("Index", "Home");
        //        }
        //        else
        //        {
        //            // Sudah pernah coba set session, tetap gagal, tampilkan error
        //            return Content("Gagal membuat session user. Silakan hubungi admin.");
        //        }
        //    }

        //    // Jika session sudah ada, cek TempData untuk returnurl
        //    if (TempData["returnurl"] != null)
        //    {
        //        string returnurlTemp = TempData["returnurl"].ToString();
        //        string decodedReturnUrl = HttpUtility.UrlDecode(returnurlTemp);

        //        if (!string.IsNullOrEmpty(decodedReturnUrl) && decodedReturnUrl.StartsWith("http://btmprd01"))
        //        {
        //            Response.Redirect(decodedReturnUrl, false);
        //            return new EmptyResult();
        //        }
        //    }
        //    // Jika ada returnurl di query string dan session sudah ada, redirect langsung
        //    else if (!string.IsNullOrEmpty(returnurl))
        //    {
        //        string decodedReturnUrl = HttpUtility.UrlDecode(returnurl);

        //        if (!string.IsNullOrEmpty(decodedReturnUrl) && decodedReturnUrl.StartsWith("http://btmprd01"))
        //        {
        //            Response.Redirect(decodedReturnUrl, false);
        //            return new EmptyResult();
        //        }
        //    }

        //    // Jika tidak ada returnurl, tampilkan Home biasa
        //    ViewBag.UserInfo = userInfo;
        //    ViewData["Title"] = "Home";
        //    return View();
        //}


        public async Task<SupervisorInfo?> GetSupervisorInfoUsingUserPrincipal(string domain, UserPrincipal userPrincipal)
        {
            try
            {
                var managerDN = (userPrincipal.GetUnderlyingObject() as DirectoryEntry)?.Properties["manager"]?.Value?.ToString();
                if (string.IsNullOrEmpty(managerDN))
                {
                    _logger.LogInformation("Manager DN tidak ditemukan.");
                    return null;
                }

                using (var managerEntry = new DirectoryEntry($"LDAP://{domain}/{managerDN}"))
                {
                    string supervisorUserName = managerEntry.Properties["sAMAccountName"]?.Value?.ToString();
                    if (string.IsNullOrEmpty(supervisorUserName))
                    {
                        _logger.LogInformation("Supervisor tidak ditemukan.");
                        return null;
                    }

                    // Cari supervisor menggunakan UserPrincipal berdasarkan sAMAccountName
                    using (var ldapContext = new PrincipalContext(ContextType.Domain, domain))
                    {
                        var supervisorPrincipal = UserPrincipal.FindByIdentity(ldapContext, IdentityType.SamAccountName, supervisorUserName);
                        if (supervisorPrincipal != null)
                        {
                            var supervisorInfo = new SupervisorInfo
                            {
                                FullName = supervisorPrincipal.DisplayName ?? "No Name",
                                Email = supervisorPrincipal.EmailAddress ?? "No Email",
                                IDCard = supervisorPrincipal.Description ?? "No Badge ID"
                            };

                            return supervisorInfo;
                        }
                        else
                        {
                            _logger.LogInformation("Supervisor tidak ditemukan menggunakan UserPrincipal.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saat mencari supervisor: {ex.Message}");
            }

            return null;
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
