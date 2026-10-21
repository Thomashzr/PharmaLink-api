using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaLink.DTOs.Insumos;
using PharmaLink.Interfaces;

namespace PharmaLink.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InsumosController(IInsumoService insumoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InsumoReadDTO>>> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var insumos = await insumoService.GetAllAsync(search, page, pageSize);
        return Ok(insumos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InsumoReadDTO>> GetById(int id)
    {
        var insumo = await insumoService.GetByIdAsync(id);
        if (insumo is null)
        {
            return NotFound(new { mensaje = $"Insumo con ID {id} no encontrado." });
        }

        return Ok(insumo);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<InsumoReadDTO>> Create([FromBody] InsumoCreateDTO dto)
    {
        var creado = await insumoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int id, [FromBody] InsumoUpdateDTO dto)
    {
        var actualizado = await insumoService.UpdateAsync(id, dto);
        if (!actualizado)
        {
            return NotFound(new { mensaje = $"Insumo con ID {id} no encontrado para actualizar." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var eliminado = await insumoService.DeleteAsync(id);
            if (!eliminado)
            {
                return NotFound(new { mensaje = $"Insumo con ID {id} no encontrado para eliminar." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}
