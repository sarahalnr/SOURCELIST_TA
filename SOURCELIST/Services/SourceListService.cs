using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using sourcelist.DTOs;
using sourcelist.Models.ViewModels;
using System; 
using System.Collections.Generic; 
using System.Data;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace sourcelist.Services
{

    public class SourceListService : ISourceListService
    {
        private readonly IConfiguration _configuration;


        public SourceListService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> CreateNewSourceListAsync(SourceListCreateViewModel model, string attachmentFileName)
        {

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            string newSourceListId = string.Empty;


            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("SOURCELIST_INSERT_NEW_SOURCELIST", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Parameter yang dikirim ke Stored Procedure
                    command.Parameters.AddWithValue("@Requestor", model.Requestor);
                    command.Parameters.AddWithValue("@RequestorEmail", model.RequestorEmail);
                    command.Parameters.AddWithValue("@BAUNumber", model.BAUNumber);
                    command.Parameters.AddWithValue("@PartDescription", model.PartDescription);
                    command.Parameters.AddWithValue("@SupplierName", model.SupplierName);
                    command.Parameters.AddWithValue("@VendorCode", model.VendorCode);
                    command.Parameters.AddWithValue("@SupplierStatus", model.SupplierStatus);
                    command.Parameters.AddWithValue("@SourceListStatus", model.SourceListStatus);
                    command.Parameters.AddWithValue("@CMSFinalCRB", model.CMSFinalCRB);
                    command.Parameters.AddWithValue("@ReasonSubmission", model.ReasonSubmission);
                    command.Parameters.AddWithValue("@ApproverName", model.ApproverName);
                    command.Parameters.AddWithValue("@ApproverEmail", model.ApproverEmail);
                    command.Parameters.AddWithValue("@EndorsementList", model.SupplierEndorsementList);


                    if (string.IsNullOrEmpty(attachmentFileName))
                    {
                        command.Parameters.AddWithValue("@AttachmentFileName", DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@AttachmentFileName", attachmentFileName);
                    }

                    await connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();

                    if (result != null)
                    {
                        newSourceListId = result.ToString();
                    }
                }
            }



            return newSourceListId;


        }
        public async Task UpdateAttachmentPathAsync(string sourceListId, string attachmentPath)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                
                using (var command = new SqlCommand("SOURCELIST_UPDATE_ATTACHMENT_PATH", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SourceListNumber", sourceListId);
                    command.Parameters.AddWithValue("@AttachmentPath", string.IsNullOrEmpty(attachmentPath) ? DBNull.Value : attachmentPath);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }



        public async Task<PagedResult<SourceListDTO>> GetSourceListsByEmailPagedAsync(string email, int page, int pageSize, string searchTerm)
        {
            var dataList = new List<SourceListDTO>();
            int totalRows = 0;

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("SOURCELIST_GET_MY_SOURCELIST", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@EmailLogin", email);
                    command.Parameters.AddWithValue("@PageNumber", page);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? DBNull.Value : searchTerm);

                    var totalRowsParam = new SqlParameter
                    {
                        ParameterName = "@TotalRows",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(totalRowsParam);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        await reader.NextResultAsync();


                        while (await reader.ReadAsync())
                        {
                            dataList.Add(new SourceListDTO
                            {
                            
                                BAUNumber = reader["BAUNumber"].ToString(),
                                PartDescription = reader["PartDescription"].ToString(),
                                SupplierName = reader["SupplierName"].ToString(),
                                SourceListStatus = reader["SourceListStatus"].ToString(),
                               
                            });

                        }
                    }


                    if (totalRowsParam.Value != DBNull.Value)
                    {
                        totalRows = (int)totalRowsParam.Value;
                    }
                }
            }

            return new PagedResult<SourceListDTO>
            {
                Data = dataList,
                TotalRows = totalRows,
                TotalPages = (int)Math.Ceiling((double)totalRows / pageSize)
            };
        }
    }
}
