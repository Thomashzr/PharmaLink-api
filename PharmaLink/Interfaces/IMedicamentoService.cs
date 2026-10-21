using PharmaLink.DTOs.Medicamentos;

namespace PharmaLink.Interfaces;

public interface IMedicamentoService
{
    Task<IEnumerable<MedicamentoReadDTO>> GetAllAsync(string? search = null, int page = 1, int pageSize = 20);
    Task<MedicamentoReadDTO?> GetByIdAsync(int id);
    Task<MedicamentoReadDTO> CreateAsync(MedicamentoCreateDTO dto);
    Task<bool> UpdateAsync(int id, MedicamentoUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}
