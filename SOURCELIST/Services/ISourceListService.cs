using sourcelist.DTOs;
using sourcelist.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sourcelist.Services
{
    public class PagedResult<T>
    {
        public List<T> Data { get; set; }
        public int TotalRows { get; set; }
        public int TotalPages { get; set; }
    }

    public interface ISourceListService
    {
        Task<string> CreateNewSourceListAsync(SourceListCreateViewModel model, string attachmentFileName);

        Task UpdateAttachmentPathAsync(string sourceListId, string attachmentPath);


        Task<PagedResult<SourceListDTO>> GetSourceListsByEmailPagedAsync(
            string email,
    int pageNumber,
    int pageSize,
    string sortColumn,
    string sortDirection,
    string searchTerm);
    }
}