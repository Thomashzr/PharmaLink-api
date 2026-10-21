using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaLink.DTOs.Reposiciones;
using PharmaLink.Interfaces;

namespace PharmaLink.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReposicionesController(IReposicionService reposicionService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ReposicionReadDTO>> Create([FromBody] ReposicionCreateDTO dto)
    {
        try
        {
            var reposicion = await reposicionService.CrearSolicitudAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = reposicion.Id }, reposicion);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ReposicionReadDTO>> GetById(int id)
    {
        var reposicion = await reposicionService.GetByIdAsync(id);
        if (reposicion is null)
        {
            return NotFound(new { mensaje = $"Reposición con ID {id} no encontrada." });
        }

        return Ok(reposicion);
    }

    [HttpPost("{id:int}/confirmar")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ReposicionReadDTO>> Confirmar(int id)
    {
        try
        {
            var reposicion = await reposicionService.ConfirmarReposicionAsync(id);
            return Ok(reposicion);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("resumen")]
    [Authorize]
    public async Task<ActionResult<ResumenReposicionDTO>> GetResumen()
    {
        var resumen = await reposicionService.ObtenerResumenAsync();
        return Ok(resumen);
    }
}
