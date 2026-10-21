using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaLink.DTOs.Medicamentos;
using PharmaLink.Interfaces;

namespace PharmaLink.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicamentosController(IMedicamentoService medicamentoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicamentoReadDTO>>> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var medicamentos = await medicamentoService.GetAllAsync(search, page, pageSize);
        return Ok(medicamentos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MedicamentoReadDTO>> GetById(int id)
    {
        var medicamento = await medicamentoService.GetByIdAsync(id);
        if (medicamento is null)
        {
            return NotFound(new { mensaje = $"Medicamento con ID {id} no encontrado." });
        }

        return Ok(medicamento);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<MedicamentoReadDTO>> Create([FromBody] MedicamentoCreateDTO dto)
    {
        var creado = await medicamentoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int id, [FromBody] MedicamentoUpdateDTO dto)
    {
        var actualizado = await medicamentoService.UpdateAsync(id, dto);
        if (!actualizado)
        {
            return NotFound(new { mensaje = $"Medicamento con ID {id} no encontrado para actualizar." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var eliminado = await medicamentoService.DeleteAsync(id);
            if (!eliminado)
            {
                return NotFound(new { mensaje = $"Medicamento con ID {id} no encontrado para eliminar." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}
