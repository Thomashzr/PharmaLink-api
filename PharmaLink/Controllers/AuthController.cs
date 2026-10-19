using Microsoft.AspNetCore.Mvc;
using PharmaLink.DTOs.Auth;
using PharmaLink.Interfaces;

namespace PharmaLink.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDTO>> Register([FromBody] RegistroDTO dto)
    {
        var response = await authService.RegisterAsync(dto);
        if (response is null)
        {
            return BadRequest(new { mensaje = $"El nombre de usuario '{dto.Usuario}' ya se encuentra registrado." });
        }

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginDTO dto)
    {
        var response = await authService.LoginAsync(dto);
        if (response is null)
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas. Verifique usuario y contraseña." });
        }

        return Ok(response);
    }
}
