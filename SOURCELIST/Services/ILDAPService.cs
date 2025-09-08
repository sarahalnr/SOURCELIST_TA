using sourcelist.Models.ViewModels;

namespace sourcelist.Services
{
    public interface ILDAPService
    {
        //List<LDAPUserViewModel> GetAllUsers();
        List<LDAPUserViewModel> GetAllUsers(string searchTerm = null);
    }
}
