using PharmaLink.DTOs.Insumos;

namespace PharmaLink.Interfaces;

public interface IInsumoService
{
    Task<IEnumerable<InsumoReadDTO>> GetAllAsync(string? search = null, int page = 1, int pageSize = 20);
    Task<InsumoReadDTO?> GetByIdAsync(int id);
    Task<InsumoReadDTO> CreateAsync(InsumoCreateDTO dto);
    Task<bool> UpdateAsync(int id, InsumoUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}
