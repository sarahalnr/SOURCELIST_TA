using sourcelist.Models;
using sourcelist.Models.ViewModels;
using Microsoft.Extensions.Options;
using sourcelist.Models.ViewModels;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace sourcelist.Services
{
    public class LDAPService : ILDAPService
    {
        private readonly string _domain;

        public LDAPService(IOptions<LDAPOptions> options)
        {
            _domain = options.Value.Domain;
        }

        private string GetLdapPath()
        {
            var parts = _domain.Split('.');
            return "LDAP://" + string.Join(",", parts.Select(p => $"DC={p}"));
        }

        public List<LDAPUserViewModel> GetAllUsers(string searchTerm = null)
        {
            var users = new List<LDAPUserViewModel>();
            string domainPath = GetLdapPath();
            DirectoryEntry searchRoot = new DirectoryEntry(domainPath);
            DirectorySearcher search = new DirectorySearcher(searchRoot);

            string filter = "(&(objectClass=user)(objectCategory=person)";
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filter += $"(mail=*{searchTerm}*)";
            }
            filter += ")";

            search.Filter = filter;
            search.PropertiesToLoad.Add("samaccountname");
            search.PropertiesToLoad.Add("displayname");
            search.PropertiesToLoad.Add("mail");
            search.PropertiesToLoad.Add("description");

            var resultCol = search.FindAll();
            if (resultCol != null)
            {
                foreach (SearchResult result in resultCol)
                {
                    if (result.Properties.Contains("samaccountname") &&
                        result.Properties.Contains("displayname"))
                    {
                        users.Add(new LDAPUserViewModel
                        {
                            UserName = (string)result.Properties["samaccountname"][0],
                            DisplayName = (string)result.Properties["displayname"][0],
                            Email = result.Properties.Contains("mail") ? (string)result.Properties["mail"][0] : "",
                            BadgeNo = result.Properties.Contains("description") ? (string)result.Properties["description"][0] : ""
                        });
                    }
                }
            }
            return users.OrderBy(u => u.DisplayName).ToList();
        }

    }
}
