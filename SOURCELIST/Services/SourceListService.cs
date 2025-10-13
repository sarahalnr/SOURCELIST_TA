using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using sourcelist.DTOs;
using sourcelist.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;


namespace sourcelist.Services
{
    public class SourceListService : ISourceListService
    {
        private readonly IConfiguration _configuration;

        public SourceListService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public async Task<string> CreateNewSourceListAsync(SourceListCreateViewModel model, string attachmentFileName, string endorsementFileName)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            string newSourceListId = string.Empty;

            using (var connection = new SqlConnection(connectionString))
            {
                // Memanggil SATU Stored Procedure utama yang melakukan semuanya
                using (var command = new SqlCommand("SOURCELIST_INSERT_NEW_SOURCELIST", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;


                    command.Parameters.AddWithValue("@RequestorId", model.RequestorId);
                    command.Parameters.AddWithValue("@ApproverId", model.ApproverId);
                    command.Parameters.AddWithValue("@SupplierId", model.SupplierId);

                    // Parameter lainnya dari form
                    command.Parameters.AddWithValue("@BAUNumber", model.BAUNumber);
                    command.Parameters.AddWithValue("@PartDescription", model.PartDescription);
                    command.Parameters.AddWithValue("@SupplierStatus", model.SupplierStatus);
                    command.Parameters.AddWithValue("@SourceListStatus", model.SourceListStatus);
                    command.Parameters.AddWithValue("@ReasonSubmission", model.ReasonSubmission);
                    command.Parameters.AddWithValue("@CMSFinalCRB", model.CMSFinalCRB);

                    // Mengirim nama file
                    command.Parameters.AddWithValue("@AttachmentFileName",
                        string.IsNullOrEmpty(attachmentFileName) ? DBNull.Value : (object)attachmentFileName);
                    command.Parameters.AddWithValue("@AttachedEndorsement",
                        string.IsNullOrEmpty(endorsementFileName) ? DBNull.Value : (object)endorsementFileName);

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


        public async Task UpdateEndorsementPathAsync(string sourceListId, string endorsementPath)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("SOURCELIST_UPDATE_ENDORSEMENT_PATH", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SourceListNumber", sourceListId);

                    command.Parameters.AddWithValue("@AttachedEndorsement", string.IsNullOrEmpty(endorsementPath) ? DBNull.Value : endorsementPath);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

     
        public async Task<PagedResult<SourceListDTO>> GetSourceListsByEmailPagedAsync(string email, int pageNumber, int pageSize, string sortColumn, string sortDirection, string searchTerm)
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
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    command.Parameters.AddWithValue("@SortColumn", sortColumn);
                    command.Parameters.AddWithValue("@SortDirection", sortDirection);
                    command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? DBNull.Value : searchTerm);

                    var totalRowsParam = new SqlParameter("@TotalRows", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(totalRowsParam);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataList.Add(new SourceListDTO
                            {
                                SourceListNumber = reader["SourceListNumber"].ToString(),
                                Requestor = reader["Requestor"].ToString(),
                                SubmittedDate = reader["SubmittedDate"] != DBNull.Value ? Convert.ToDateTime(reader["SubmittedDate"]) : default,
                                BAUNumber = reader["BAUNumber"].ToString(),
                                PartDescription = reader["PartDescription"].ToString(),
                                SupplierName = reader["SupplierName"].ToString(),
                                VendorCode = reader["VendorCode"].ToString(),
                                ReasonSubmission = reader["ReasonSubmission"].ToString(),
                                SourceListStatus = reader["SourceListStatus"].ToString(),
                                ApproverStatus = reader["ApprovalStatus"]?.ToString()
                            });
                        }
                    }

                    if (totalRowsParam.Value != DBNull.Value)
                    {
                        totalRows = (int)totalRowsParam.Value;
                    }
                }
            }
            return new PagedResult<SourceListDTO> { Data = dataList, TotalRows = totalRows, TotalPages = (int)Math.Ceiling((double)totalRows / pageSize) };
        }

      
        public async Task<PagedResult<SourceListDTO>> GetSourceListsForApprovalPagedAsync(string email, int pageNumber, int pageSize, string sortColumn, string sortDirection, string searchTerm)
        {
            var dataList = new List<SourceListDTO>();
            int totalRows = 0;
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
            
                using (var command = new SqlCommand("SOURCELIST_GET_SOURCELIST_FOR_APPROVE", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@EmailLogin", email);
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    command.Parameters.AddWithValue("@SortColumn", sortColumn);
                    command.Parameters.AddWithValue("@SortDirection", sortDirection);
                    command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? DBNull.Value : searchTerm);

                    var totalRowsParam = new SqlParameter("@TotalRows", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(totalRowsParam);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataList.Add(new SourceListDTO
                            {
                                SourceListNumber = reader["SourceListNumber"].ToString(),
                                Requestor = reader["Requestor"].ToString(),
                                SubmittedDate = reader["SubmittedDate"] != DBNull.Value ? Convert.ToDateTime(reader["SubmittedDate"]) : default,
                                BAUNumber = reader["BAUNumber"].ToString(),
                                PartDescription = reader["PartDescription"].ToString(),
                                SupplierName = reader["SupplierName"].ToString(),
                                VendorCode = reader["VendorCode"].ToString(),
                                ReasonSubmission = reader["ReasonSubmission"].ToString(),
                                SourceListStatus = reader["SourceListStatus"].ToString(),
                                ApproverStatus = reader["ApprovalStatus"]?.ToString()
                            });
                        }
                    }
                    if (totalRowsParam.Value != DBNull.Value)
                    {
                        totalRows = (int)totalRowsParam.Value;
                    }
                }
            }
            return new PagedResult<SourceListDTO> { Data = dataList, TotalRows = totalRows, TotalPages = (int)Math.Ceiling((double)totalRows / pageSize) };
        }

        public async Task<SourceListDetailViewModel> GetSourceListDetailAsync(string sourceListNumber)
        {
            SourceListDetailViewModel viewModel = null;
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("SOURCELIST_GET_DETAIL", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SourceListNumber", sourceListNumber);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync()) 
                        {
                            viewModel = new SourceListDetailViewModel
                            {
                                SourceListNumber = reader["SourceListNumber"].ToString(),
                                Requestor = reader["Requestor"].ToString(),
                                BAUNumber = reader["BAUNumber"].ToString(),
                                PartDescription = reader["PartDescription"].ToString(),
                                SupplierName = reader["SupplierName"].ToString(),
                                VendorCode = reader["KodeVendor"].ToString(),
                                SupplierStatus = reader["SupplierStatus"].ToString(),
                                SourceListStatus = reader["SourceListStatus"].ToString(),
                                CMSFinalCRB = reader["CMSFinalCRB"].ToString(),
                                ReasonSubmission = reader["ReasonSubmission"].ToString(),
                                ApproverStatus = reader["ApprovalStatus"]?.ToString(),
                                ApproverName = reader["ApproverName"].ToString(),
                                ApproverEmail = reader["ApproverEmail"].ToString(),
                                AttachmentFileName = reader["AttachmentFileName"]?.ToString(), 
                                AttachedEndorsement = reader["AttachedEndorsement"]?.ToString(),
                                SubmittedDate = reader["SubmittedDate"] != DBNull.Value ? Convert.ToDateTime(reader["SubmittedDate"]) : null,
                                ValidityPeriod = reader["ValidityPeriod"]?.ToString()

                            };
                        }
                    }
                }
            }
            return viewModel; 
        }

        public async Task ApproveSourceListAsync(ApprovalViewModel model)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("SOURCELIST_APPROVE", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SourceListNumber", model.SourceListNumber);
                    command.Parameters.AddWithValue("@ValidityPeriod", model.ValidityPeriod);
                    //command.Parameters.AddWithValue("@Remark", (object)model.Remark ?? DBNull.Value);
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task RejectSourceListAsync(ApprovalViewModel model)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("SOURCELIST_REJECT", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SourceListNumber", model.SourceListNumber);
                    //command.Parameters.AddWithValue("@Remark", model.Remark);
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<PagedResult<SourceListDTO>> GetSourceListsForAllSourceListPagedAsync(string email, int pageNumber, int pageSize, string sortColumn, string sortDirection, string searchTerm)
        {
            var dataList = new List<SourceListDTO>();
            int totalRows = 0;
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {

                using (var command = new SqlCommand("SOURCELIST_GET_ALL_SOURCELIST", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@EmailLogin", email);
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    command.Parameters.AddWithValue("@SortColumn", sortColumn);
                    command.Parameters.AddWithValue("@SortDirection", sortDirection);
                    command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? DBNull.Value : searchTerm);

                    var totalRowsParam = new SqlParameter("@TotalRows", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(totalRowsParam);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataList.Add(new SourceListDTO
                            {
                                SourceListNumber = reader["SourceListNumber"].ToString(),
                                Requestor = reader["Requestor"].ToString(),
                                SubmittedDate = reader["SubmittedDate"] != DBNull.Value ? Convert.ToDateTime(reader["SubmittedDate"]) : default,
                                BAUNumber = reader["BAUNumber"].ToString(),
                                PartDescription = reader["PartDescription"].ToString(),
                                SupplierName = reader["SupplierName"].ToString(),
                                VendorCode = reader["VendorCode"].ToString(),
                                ReasonSubmission = reader["ReasonSubmission"].ToString(),
                                SourceListStatus = reader["SourceListStatus"].ToString(),
                                ApproverStatus = reader["ApprovalStatus"]?.ToString()
                            });
                        }
                    }

                    if (totalRowsParam.Value != DBNull.Value)
                    {
                        totalRows = (int)totalRowsParam.Value;
                    }
                }
            }
            return new PagedResult<SourceListDTO> { Data = dataList, TotalRows = totalRows, TotalPages = (int)Math.Ceiling((double)totalRows / pageSize) };
        }


    }

}
