using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using sourcelist.DTOs;
using sourcelist.Services; 
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

public class UserService : IUserService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public UserService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("DefaultConnection");
    }

    public async Task CreateUserAsync(UserDTO userDto)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand("USER_CREATE_TA", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Username", userDto.Username);
                command.Parameters.AddWithValue("@Password", BC.HashPassword(userDto.Password));
                command.Parameters.AddWithValue("@Email", userDto.Email);
                command.Parameters.AddWithValue("@Role", userDto.Role);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<PagedResult<UserDTO>> GetAllUsersPagedAsync(int pageNumber, int pageSize, string searchTerm)
    {
        var users = new List<UserDTO>();
        int totalRows = 0;

        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand("USER_GET_ALL_PAGED_TA", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@PageNumber", pageNumber);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? DBNull.Value : searchTerm);

                var totalRowsParam = new SqlParameter("@TotalRows", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(totalRowsParam);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(new UserDTO
                        {
                            ID_User = (int)reader["ID_User"],
                            Username = reader["Username"].ToString(),
                            Email = reader["Email"].ToString(),
                            Role = reader["Role"].ToString(),
                            Status = reader["Status"].ToString()
                        });
                    }
                }
                totalRows = (int)totalRowsParam.Value;
            }
        }
        return new PagedResult<UserDTO> { Data = users, TotalRows = totalRows };
    }

    public async Task UpdateUserAsync(UserDTO userDto)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand("USER_UPDATE_TA", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ID_User", userDto.ID_User);
                command.Parameters.AddWithValue("@Username", userDto.Username);
                command.Parameters.AddWithValue("@Email", userDto.Email);
                command.Parameters.AddWithValue("@Role", userDto.Role);
                command.Parameters.AddWithValue("@Status", userDto.Status);

                // Hanya kirim parameter password jika diisi
                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    command.Parameters.AddWithValue("@Password", BC.HashPassword(userDto.Password));
                }
                else
                {
                    command.Parameters.AddWithValue("@Password", DBNull.Value);
                }

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}