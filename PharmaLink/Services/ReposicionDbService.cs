using Microsoft.EntityFrameworkCore;
using PharmaLink.Context;
using PharmaLink.DTOs.Reposiciones;
using PharmaLink.Interfaces;
using PharmaLink.Models;

namespace PharmaLink.Services;

public class ReposicionDbService(
    PharmaLinkContext context,
    ILogger<ReposicionDbService> logger
) : IReposicionService
{
    public async Task<ReposicionReadDTO> CrearSolicitudAsync(ReposicionCreateDTO dto)
    {
        logger.LogInformation("Iniciando creación de solicitud de reposición con {CantidadItems} ítems...", dto.Items.Count);

        var insumoIds = dto.Items.Select(i => i.InsumoId).Distinct().ToList();
        var insumosExistentes = await context.Insumos
            .Where(i => insumoIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Nombre);

        var faltantes = insumoIds.Except(insumosExistentes.Keys).ToList();
        if (faltantes.Count != 0)
        {
            logger.LogWarning("Insumos inexistentes en la solicitud: {Faltantes}", string.Join(", ", faltantes));
            throw new KeyNotFoundException($"Los siguientes IDs de insumos no existen en el catálogo: {string.Join(", ", faltantes)}.");
        }

        var reposicion = new Reposicion
        {
            FechaSolicitud = DateTime.UtcNow,
            Estado = EstadoReposicion.Solicitada
        };

        foreach (var item in dto.Items)
        {
            reposicion.Entregas.Add(new Entrega
            {
                InsumoId = item.InsumoId,
                Cantidad = item.Cantidad,
                FechaEntrega = DateTime.UtcNow
            });
        }

        context.Reposiciones.Add(reposicion);
        await context.SaveChangesAsync();

        logger.LogInformation("Solicitud de reposición {Id} creada en estado Solicitada (el stock de insumos no se altera aún).", reposicion.Id);

        return new ReposicionReadDTO
        {
            Id = reposicion.Id,
            FechaSolicitud = reposicion.FechaSolicitud,
            FechaConfirmacion = reposicion.FechaConfirmacion,
            Estado = reposicion.Estado.ToString(),
            Entregas = reposicion.Entregas.Select(e => new EntregaDetalleDTO
            {
                Id = e.Id,
                InsumoId = e.InsumoId,
                NombreInsumo = insumosExistentes[e.InsumoId],
                Cantidad = e.Cantidad,
                FechaEntrega = e.FechaEntrega
            }).ToList()
        };
    }

    public async Task<ReposicionReadDTO?> GetByIdAsync(int id)
    {
        return await context.Reposiciones
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReposicionReadDTO
            {
                Id = r.Id,
                FechaSolicitud = r.FechaSolicitud,
                FechaConfirmacion = r.FechaConfirmacion,
                Estado = r.Estado.ToString(),
                Entregas = r.Entregas.Select(e => new EntregaDetalleDTO
                {
                    Id = e.Id,
                    InsumoId = e.InsumoId,
                    NombreInsumo = e.Insumo != null ? e.Insumo.Nombre : string.Empty,
                    Cantidad = e.Cantidad,
                    FechaEntrega = e.FechaEntrega
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ReposicionReadDTO> ConfirmarReposicionAsync(int id)
    {
        logger.LogInformation("Iniciando confirmación de reposición {Id}...", id);

        var reposicion = await context.Reposiciones
            .Include(r => r.Entregas)
            .ThenInclude(e => e.Insumo)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reposicion is null)
        {
            logger.LogWarning("Reposición con ID {Id} no encontrada para confirmar.", id);
            throw new KeyNotFoundException($"La reposición con ID {id} no fue encontrada.");
        }

        if (reposicion.Estado != EstadoReposicion.Solicitada)
        {
            logger.LogWarning("Intento de confirmación inválido: Reposición {Id} en estado {Estado}.", id, reposicion.Estado);
            throw new InvalidOperationException($"La reposición {id} no se encuentra en estado 'Solicitada' (estado actual: {reposicion.Estado}).");
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            reposicion.Estado = EstadoReposicion.Confirmada;
            reposicion.FechaConfirmacion = DateTime.UtcNow;

            foreach (var entrega in reposicion.Entregas)
            {
                var insumo = await context.Insumos.FindAsync(entrega.InsumoId);
                if (insumo != null)
                {
                    insumo.StockDisponible += entrega.Cantidad;
                    logger.LogInformation("Stock de insumo {InsumoId} ('{Nombre}') incrementado en +{Cantidad}. Nuevo stock: {Stock}.",
                        insumo.Id, insumo.Nombre, entrega.Cantidad, insumo.StockDisponible);
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Reposición {Id} confirmada con éxito. Stocks actualizados.", reposicion.Id);

            return new ReposicionReadDTO
            {
                Id = reposicion.Id,
                FechaSolicitud = reposicion.FechaSolicitud,
                FechaConfirmacion = reposicion.FechaConfirmacion,
                Estado = reposicion.Estado.ToString(),
                Entregas = reposicion.Entregas.Select(e => new EntregaDetalleDTO
                {
                    Id = e.Id,
                    InsumoId = e.InsumoId,
                    NombreInsumo = e.Insumo != null ? e.Insumo.Nombre : string.Empty,
                    Cantidad = e.Cantidad,
                    FechaEntrega = e.FechaEntrega
                }).ToList()
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ResumenReposicionDTO> ObtenerResumenAsync()
    {
        logger.LogInformation("Calculando resumen de reposición e inventario de insumos...");

        var totalSolicitudes = await context.Reposiciones.CountAsync();
        var totalConfirmadas = await context.Reposiciones.CountAsync(r => r.Estado == EstadoReposicion.Confirmada);
        var totalPendientes = await context.Reposiciones.CountAsync(r => r.Estado == EstadoReposicion.Solicitada);

        var stockInsumos = await context.Insumos
            .AsNoTracking()
            .OrderBy(i => i.Nombre)
            .Select(i => new InsumoStockDTO
            {
                InsumoId = i.Id,
                Nombre = i.Nombre,
                StockActual = i.StockDisponible
            })
            .ToListAsync();

        return new ResumenReposicionDTO
        {
            TotalSolicitudes = totalSolicitudes,
            TotalConfirmadas = totalConfirmadas,
            TotalPendientes = totalPendientes,
            StockInsumos = stockInsumos
        };
    }
}
