using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaLink.DTOs.Dispensaciones;
using PharmaLink.Exceptions;
using PharmaLink.Interfaces;

namespace PharmaLink.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DispensacionesController(IDispensacionService dispensacionService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DispensacionReadDTO>> Create([FromBody] DispensacionCreateDTO dto)
    {
        try
        {
            var dispensacion = await dispensacionService.RegistrarDispensacionAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = dispensacion.Id }, dispensacion);
        }
        catch (RecetaInvalidaException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (StockInsuficienteException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                mensaje = "Falla de comunicación con el sistema hospitalario externo.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DispensacionReadDTO>> GetById(int id)
    {
        var dispensacion = await dispensacionService.GetByIdAsync(id);
        if (dispensacion is null)
        {
            return NotFound(new { mensaje = $"Dispensación con ID {id} no encontrada." });
        }

        return Ok(dispensacion);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DispensacionReadDTO>>> GetAll()
    {
        var dispensaciones = await dispensacionService.GetAllAsync();
        return Ok(dispensaciones);
    }
}
