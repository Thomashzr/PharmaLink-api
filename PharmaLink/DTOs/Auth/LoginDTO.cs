using System.ComponentModel.DataAnnotations;

namespace PharmaLink.DTOs.Auth;

public record LoginDTO(
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    string Usuario,

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    string Password
);
