using Microsoft.EntityFrameworkCore;
using PharmaLink.Context;
using PharmaLink.DTOs.Insumos;
using PharmaLink.Interfaces;
using PharmaLink.Models;

namespace PharmaLink.Services;

public class InsumoDbService(
    PharmaLinkContext context,
    ILogger<InsumoDbService> logger
) : IInsumoService
{
    public async Task<IEnumerable<InsumoReadDTO>> GetAllAsync(string? search = null, int page = 1, int pageSize = 20)
    {
        var query = context.Insumos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(i => i.Nombre.ToLower().Contains(term) || i.Presentacion.ToLower().Contains(term));
        }

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        logger.LogInformation("Consultando insumos con filtro '{Search}', página {Page}, tamaño {PageSize}.", search, normalizedPage, normalizedPageSize);

        return await query
            .OrderBy(i => i.Nombre)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(i => new InsumoReadDTO
            {
                Id = i.Id,
                Nombre = i.Nombre,
                Presentacion = i.Presentacion,
                StockDisponible = i.StockDisponible,
                PrecioUnitario = i.PrecioUnitario
            })
            .ToListAsync();
    }

    public async Task<InsumoReadDTO?> GetByIdAsync(int id)
    {
        return await context.Insumos
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new InsumoReadDTO
            {
                Id = i.Id,
                Nombre = i.Nombre,
                Presentacion = i.Presentacion,
                StockDisponible = i.StockDisponible,
                PrecioUnitario = i.PrecioUnitario
            })
            .FirstOrDefaultAsync();
    }

    public async Task<InsumoReadDTO> CreateAsync(InsumoCreateDTO dto)
    {
        var entity = new Insumo
        {
            Nombre = dto.Nombre.Trim(),
            Presentacion = dto.Presentacion.Trim(),
            StockDisponible = dto.StockDisponible,
            PrecioUnitario = dto.PrecioUnitario
        };

        context.Insumos.Add(entity);
        await context.SaveChangesAsync();

        logger.LogInformation("Insumo {Id} ('{Nombre}') creado exitosamente.", entity.Id, entity.Nombre);

        return new InsumoReadDTO
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            Presentacion = entity.Presentacion,
            StockDisponible = entity.StockDisponible,
            PrecioUnitario = entity.PrecioUnitario
        };
    }

    public async Task<bool> UpdateAsync(int id, InsumoUpdateDTO dto)
    {
        var entity = await context.Insumos.FindAsync(id);
        if (entity is null)
        {
            logger.LogWarning("Intento de actualización fallido: Insumo {Id} no encontrado.", id);
            return false;
        }

        entity.Nombre = dto.Nombre.Trim();
        entity.Presentacion = dto.Presentacion.Trim();
        entity.StockDisponible = dto.StockDisponible;
        entity.PrecioUnitario = dto.PrecioUnitario;

        await context.SaveChangesAsync();
        logger.LogInformation("Insumo {Id} ('{Nombre}') actualizado exitosamente.", entity.Id, entity.Nombre);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await context.Insumos.FindAsync(id);
        if (entity is null)
        {
            logger.LogWarning("Intento de eliminación fallido: Insumo {Id} no encontrado.", id);
            return false;
        }

        var tieneEntregas = await context.Entregas.AnyAsync(e => e.InsumoId == id);
        if (tieneEntregas)
        {
            logger.LogWarning("Intento de eliminación rechazado: Insumo {Id} posee entregas de reposición asociadas.", id);
            throw new InvalidOperationException("Violación de integridad referencial: el insumo tiene entregas asociadas y no puede ser eliminado.");
        }

        context.Insumos.Remove(entity);
        await context.SaveChangesAsync();

        logger.LogInformation("Insumo {Id} ('{Nombre}') eliminado exitosamente.", id, entity.Nombre);
        return true;
    }
}
