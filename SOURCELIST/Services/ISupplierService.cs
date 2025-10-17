using sourcelist.DTOs;
using System.Threading.Tasks;

namespace sourcelist.Services
{
    public interface ISupplierService
    {
        Task<PagedResult<SupplierDTO>> GetAllSuppliersPagedAsync(int pageNumber, int pageSize, string searchTerm);
        Task CreateSupplierAsync(SupplierDTO supplierDto);
        Task UpdateSupplierAsync(SupplierDTO supplierDto);
    }
}