using sourcelist.Models.ViewModels;
using System.Threading.Tasks; 

namespace sourcelist.Services
{
    public interface ISourceListService
    {
        Task<string> CreateNewSourceListAsync(SourceListCreateViewModel model, string attachmentFileName);
    }
}