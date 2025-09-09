using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration; 
using sourcelist.Models.ViewModels; 
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
    }
}