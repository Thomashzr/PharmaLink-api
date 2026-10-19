using System.ComponentModel.DataAnnotations;

namespace PharmaLink.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Rol { get; set; } = "Usuario"; // "Usuario" o "Administrador"
}
