using sourcelist.Services;
using Microsoft.AspNetCore.Mvc;

namespace sourcelist.Controllers
{
    public class SourceList : Controller
    {

        private readonly ILDAPService _ldapService;

  
        public SourceList(ILDAPService ldapService)
        {
            _ldapService = ldapService;
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public JsonResult SearchApprovers(string term)
        {
            var users = _ldapService.GetAllUsers(term);
            var result = users.Select(u => new
            {
                id = u.UserName,
                text = $"{u.DisplayName}",
                //text = $"{u.DisplayName} ({u.UserName})",
                email = u.Email,
                desc = u.BadgeNo
            });
            return Json(result);
        }
    }
}