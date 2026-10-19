using PharmaLink.DTOs.Auth;

namespace PharmaLink.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO?> RegisterAsync(RegistroDTO dto);
    Task<AuthResponseDTO?> LoginAsync(LoginDTO dto);
}
