using PharmaLink.DTOs.External;

namespace PharmaLink.Interfaces;

public interface IHospitalApiClient
{
    Task<RecetaValidacionResultDTO> ValidarRecetaAsync(string codigoReceta);
}
