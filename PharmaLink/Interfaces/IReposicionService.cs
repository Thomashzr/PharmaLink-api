using PharmaLink.DTOs.Reposiciones;

namespace PharmaLink.Interfaces;

public interface IReposicionService
{
    Task<ReposicionReadDTO> CrearSolicitudAsync(ReposicionCreateDTO dto);
    Task<ReposicionReadDTO?> GetByIdAsync(int id);
    Task<ReposicionReadDTO> ConfirmarReposicionAsync(int id);
    Task<ResumenReposicionDTO> ObtenerResumenAsync();
}
