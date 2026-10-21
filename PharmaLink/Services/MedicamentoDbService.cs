using Microsoft.EntityFrameworkCore;
using PharmaLink.Context;
using PharmaLink.DTOs.Medicamentos;
using PharmaLink.Interfaces;
using PharmaLink.Models;

namespace PharmaLink.Services;

public class MedicamentoDbService(
    PharmaLinkContext context,
    ILogger<MedicamentoDbService> logger
) : IMedicamentoService
{
    public async Task<IEnumerable<MedicamentoReadDTO>> GetAllAsync(string? search = null, int page = 1, int pageSize = 20)
    {
        var query = context.Medicamentos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m => m.Nombre.ToLower().Contains(term) || m.Presentacion.ToLower().Contains(term));
        }

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        logger.LogInformation("Consultando medicamentos con filtro '{Search}', página {Page}, tamaño {PageSize}.", search, normalizedPage, normalizedPageSize);

        return await query
            .OrderBy(m => m.Nombre)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(m => new MedicamentoReadDTO
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Presentacion = m.Presentacion,
                StockDisponible = m.StockDisponible,
                PrecioUnitario = m.PrecioUnitario
            })
            .ToListAsync();
    }

    public async Task<MedicamentoReadDTO?> GetByIdAsync(int id)
    {
        return await context.Medicamentos
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MedicamentoReadDTO
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Presentacion = m.Presentacion,
                StockDisponible = m.StockDisponible,
                PrecioUnitario = m.PrecioUnitario
            })
            .FirstOrDefaultAsync();
    }

    public async Task<MedicamentoReadDTO> CreateAsync(MedicamentoCreateDTO dto)
    {
        var entity = new Medicamento
        {
            Nombre = dto.Nombre.Trim(),
            Presentacion = dto.Presentacion.Trim(),
            StockDisponible = dto.StockDisponible,
            PrecioUnitario = dto.PrecioUnitario
        };

        context.Medicamentos.Add(entity);
        await context.SaveChangesAsync();

        logger.LogInformation("Medicamento {Id} ('{Nombre}') creado exitosamente.", entity.Id, entity.Nombre);

        return new MedicamentoReadDTO
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            Presentacion = entity.Presentacion,
            StockDisponible = entity.StockDisponible,
            PrecioUnitario = entity.PrecioUnitario
        };
    }

    public async Task<bool> UpdateAsync(int id, MedicamentoUpdateDTO dto)
    {
        var entity = await context.Medicamentos.FindAsync(id);
        if (entity is null)
        {
            logger.LogWarning("Intento de actualización fallido: Medicamento {Id} no encontrado.", id);
            return false;
        }

        entity.Nombre = dto.Nombre.Trim();
        entity.Presentacion = dto.Presentacion.Trim();
        entity.StockDisponible = dto.StockDisponible;
        entity.PrecioUnitario = dto.PrecioUnitario;

        await context.SaveChangesAsync();
        logger.LogInformation("Medicamento {Id} ('{Nombre}') actualizado exitosamente.", entity.Id, entity.Nombre);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await context.Medicamentos.FindAsync(id);
        if (entity is null)
        {
            logger.LogWarning("Intento de eliminación fallido: Medicamento {Id} no encontrado.", id);
            return false;
        }

        var tieneDispensaciones = await context.Dispensaciones.AnyAsync(d => d.MedicamentoId == id);
        if (tieneDispensaciones)
        {
            logger.LogWarning("Intento de eliminación rechazado: Medicamento {Id} posee dispensaciones históricas registradas.", id);
            throw new InvalidOperationException("Violación de integridad referencial: el medicamento tiene dispensaciones históricas registradas y no puede ser eliminado.");
        }

        context.Medicamentos.Remove(entity);
        await context.SaveChangesAsync();

        logger.LogInformation("Medicamento {Id} ('{Nombre}') eliminado exitosamente.", id, entity.Nombre);
        return true;
    }
}
