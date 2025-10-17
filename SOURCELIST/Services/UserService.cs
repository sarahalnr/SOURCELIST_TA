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
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task CreateUserAsync(UserDTO userDto)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            // Panggil SP USER_CRUD dengan TransType 'CREATE'
            using (var command = new SqlCommand("USER_CRUD", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@TransType", "CREATE");
                command.Parameters.AddWithValue("@Username", userDto.Username);
                command.Parameters.AddWithValue("@UserPassword", BC.HashPassword(userDto.Password)); 
                command.Parameters.AddWithValue("@Email", userDto.Email);
                command.Parameters.AddWithValue("@Role", userDto.Role);
              

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task UpdateUserAsync(UserDTO userDto)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            // Panggil SP USER_CRUD dengan TransType 'UPDATE'
            using (var command = new SqlCommand("USER_CRUD", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@TransType", "UPDATE");
                command.Parameters.AddWithValue("@UserID", userDto.ID_User); 
                command.Parameters.AddWithValue("@Username", userDto.Username);
                command.Parameters.AddWithValue("@Email", userDto.Email);
                command.Parameters.AddWithValue("@Role", userDto.Role);
                command.Parameters.AddWithValue("@Status", userDto.Status);

                // Hanya kirim parameter password jika diisi 
                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    command.Parameters.AddWithValue("@UserPassword", BC.HashPassword(userDto.Password));
                }
                else
                {
                    command.Parameters.AddWithValue("@UserPassword", DBNull.Value);
                }

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<UserDTO?> AuthenticateAsync(string email, string password)
    {
        UserDTO? user = null;
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand("Get_USER_LOGIN", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Email", email);
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        if (reader["Status"].ToString()!.Trim().Equals("Aktif", StringComparison.OrdinalIgnoreCase))
                        {
                            string hashedPassword = reader["UserPassword"].ToString()!;
                            if (BC.Verify(password, hashedPassword))
                            {
                                user = new UserDTO
                                {
                                    ID_User = Convert.ToInt32(reader["UserID"]),
                                    Username = reader["Username"].ToString()!,
                                    Email = reader["Email"].ToString()!,
                                    Role = reader["Role"].ToString()!,
                                    Status = reader["Status"].ToString()!
                                };
                            }
                        }
                    }
                }
            }
        }
        return user;
    }

    public async Task<PagedResult<UserDTO>> GetAllUsersPagedAsync(int pageNumber, int pageSize, string searchTerm)
    {
        var users = new List<UserDTO>();
        var totalRows = 0;
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand("USER_GET_ALL_PAGED", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@PageNumber", pageNumber);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? DBNull.Value : searchTerm);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(new UserDTO
                        {
                            ID_User = Convert.ToInt32(reader["UserID"]),
                            Username = reader["Username"].ToString() ?? "",
                            Email = reader["Email"].ToString() ?? "",
                            Role = reader["Role"].ToString() ?? "",
                            Status = reader["Status"].ToString() ?? ""
                        });
                    }
                    if (await reader.NextResultAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            totalRows = Convert.ToInt32(reader["TotalRows"]);
                        }
                    }
                }
            }
        }
        return new PagedResult<UserDTO> { Data = users, TotalRows = totalRows };
    }
}