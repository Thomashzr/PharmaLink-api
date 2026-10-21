using PharmaLink.DTOs.Dispensaciones;

namespace PharmaLink.Interfaces;

public interface IDispensacionService
{
    Task<DispensacionReadDTO> RegistrarDispensacionAsync(DispensacionCreateDTO dto);
    Task<DispensacionReadDTO?> GetByIdAsync(int id);
    Task<IEnumerable<DispensacionReadDTO>> GetAllAsync();
}
