using sourcelist.DTOs;
using sourcelist.Services; 
using System.Threading.Tasks;

public interface IUserService
{
    Task<PagedResult<UserDTO>> GetAllUsersPagedAsync(int pageNumber, int pageSize, string searchTerm);
    Task CreateUserAsync(UserDTO userDto);
    Task UpdateUserAsync(UserDTO userDto);
}