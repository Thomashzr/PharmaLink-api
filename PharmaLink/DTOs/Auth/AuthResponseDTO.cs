namespace PharmaLink.DTOs.Auth;

public record AuthResponseDTO(
    string Token,
    string Usuario,
    string Rol
);
