using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using sourcelist.DTOs;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace sourcelist.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly string _connectionString;

        public SupplierService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task CreateSupplierAsync(SupplierDTO supplierDto)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("SUPPLIER_CRUD", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@TransType", "CREATE");
                    command.Parameters.AddWithValue("@NamaSupplier", supplierDto.NamaSupplier);
                    command.Parameters.AddWithValue("@KodeVendor", supplierDto.KodeVendor);
                    command.Parameters.AddWithValue("@EmailSupplier", supplierDto.EmailSupplier);
                    command.Parameters.AddWithValue("@PICName", supplierDto.PICName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PICEmail", supplierDto.PICEmail ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Status", "Aktif");

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UpdateSupplierAsync(SupplierDTO supplierDto)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("SUPPLIER_CRUD", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@TransType", "UPDATE");
                    command.Parameters.AddWithValue("@ID_Supplier", supplierDto.ID_Supplier);
                    command.Parameters.AddWithValue("@NamaSupplier", supplierDto.NamaSupplier);
                    command.Parameters.AddWithValue("@KodeVendor", supplierDto.KodeVendor);
                    command.Parameters.AddWithValue("@EmailSupplier", supplierDto.EmailSupplier);
                    command.Parameters.AddWithValue("@PICName", supplierDto.PICName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PICEmail", supplierDto.PICEmail ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Status", supplierDto.Status);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<PagedResult<SupplierDTO>> GetAllSuppliersPagedAsync(int pageNumber, int pageSize, string searchTerm)
        {
            var suppliers = new List<SupplierDTO>();
            var totalRows = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("SUPPLIER_GET_ALL_PAGED", connection))
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
                            suppliers.Add(new SupplierDTO
                            {
                                ID_Supplier = Convert.ToInt32(reader["ID_Supplier"]),
                                NamaSupplier = reader["NamaSupplier"].ToString() ?? "",
                                KodeVendor = reader["KodeVendor"].ToString() ?? "",
                                EmailSupplier = reader["EmailSupplier"].ToString() ?? "",
                                PICName = reader["PICName"] != DBNull.Value ? reader["PICName"].ToString() : "",
                                PICEmail = reader["PICEmail"] != DBNull.Value ? reader["PICEmail"].ToString() : "",
                                Status = reader["Status"].ToString() ?? ""
                            });
                        }
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            totalRows = Convert.ToInt32(reader["TotalRows"]);
                        }
                    }
                }
            }
            return new PagedResult<SupplierDTO> { Data = suppliers, TotalRows = totalRows };
        }
    }
}