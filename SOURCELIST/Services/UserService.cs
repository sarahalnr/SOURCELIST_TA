using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using sourcelist.Data;
using sourcelist.DTOs;
using sourcelist.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

public class UserService : IUserService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    private readonly ApplicationDbContext _context;

    public UserService(IConfiguration configuration , ApplicationDbContext context)
    {
        _configuration = configuration;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _context = context;
    }

    public async Task CreateUserAsync(UserDTO userDto)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand("USER_CRUD", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@TransType", "CREATE");
                command.Parameters.AddWithValue("@Username", userDto.Username);
                command.Parameters.AddWithValue("@UserPassword", BC.HashPassword(userDto.Password));
                command.Parameters.AddWithValue("@Email", userDto.Email);
                command.Parameters.AddWithValue("@Role", userDto.Role);

                await connection.OpenAsync();

                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 2627 || ex.Number == 2601)
                    {
                        throw new Exception($"Email {userDto.Email} sudah terdaftar. Silakan gunakan email lain.");
                    }
                    else
                    {
                        throw;
                    }
                }
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

                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 2627 || ex.Number == 2601)
                    {
                        throw new Exception($"Email {userDto.Email} sudah terdaftar. Silakan gunakan email lain.");
                    }
                    else
                    {
                        throw;
                    }
                }
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

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        // Gunakan BC.Verify untuk membandingkan Plain Text vs Hash

        bool isPasswordCorrect = BC.Verify(oldPassword, user.UserPassword);

        if (!isPasswordCorrect)
        {
            return false; // Password lama salah
        }
        user.UserPassword = BC.HashPassword(newPassword);

        _context.Update(user);
        await _context.SaveChangesAsync();

        return true; 
    }


}