using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PharmaLink.Context;
using PharmaLink.DTOs.Auth;
using PharmaLink.Interfaces;
using PharmaLink.Models;

namespace PharmaLink.Services;

public class AuthService(
    PharmaLinkContext context,
    IConfiguration configuration,
    ILogger<AuthService> logger
) : IAuthService
{
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public async Task<AuthResponseDTO?> RegisterAsync(RegistroDTO dto)
    {
        var existe = await context.Usuarios
            .AnyAsync(u => u.NombreUsuario.ToLower() == dto.Usuario.Trim().ToLower());

        if (existe)
        {
            logger.LogWarning("Intento de registro fallido: el usuario {Usuario} ya existe.", dto.Usuario);
            return null;
        }

        var nuevoUsuario = new Usuario
        {
            NombreUsuario = dto.Usuario.Trim(),
            Rol = "Usuario"
        };

        nuevoUsuario.PasswordHash = _passwordHasher.HashPassword(nuevoUsuario, dto.Password);

        context.Usuarios.Add(nuevoUsuario);
        await context.SaveChangesAsync();

        logger.LogInformation("Usuario {Usuario} registrado con éxito con rol {Rol}.", nuevoUsuario.NombreUsuario, nuevoUsuario.Rol);

        var token = GenerarJwtToken(nuevoUsuario);
        return new AuthResponseDTO(token, nuevoUsuario.NombreUsuario, nuevoUsuario.Rol);
    }

    public async Task<AuthResponseDTO?> LoginAsync(LoginDTO dto)
    {
        var usuario = await context.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == dto.Usuario.Trim().ToLower());

        if (usuario is null)
        {
            logger.LogWarning("Intento de login fallido: usuario {Usuario} no encontrado.", dto.Usuario);
            return null;
        }

        var verificacion = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Intento de login fallido: contraseña incorrecta para usuario {Usuario}.", dto.Usuario);
            return null;
        }

        logger.LogInformation("Usuario {Usuario} autenticado correctamente.", usuario.NombreUsuario);

        var token = GenerarJwtToken(usuario);
        return new AuthResponseDTO(token, usuario.NombreUsuario, usuario.Rol);
    }

    private string GenerarJwtToken(Usuario usuario)
    {
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                        ?? configuration["Jwt:SecretKey"]
                        ?? "PharmaLinkSuperSecretKey2026Minimum32CharsLength!";

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Role, usuario.Rol),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
