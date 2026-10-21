using Microsoft.EntityFrameworkCore;
using PharmaLink.Context;
using PharmaLink.DTOs.Dispensaciones;
using PharmaLink.Exceptions;
using PharmaLink.Interfaces;
using PharmaLink.Models;

namespace PharmaLink.Services;

public class DispensacionDbService(
    PharmaLinkContext context,
    IHospitalApiClient hospitalApiClient,
    ILogger<DispensacionDbService> logger
) : IDispensacionService
{
    public async Task<DispensacionReadDTO> RegistrarDispensacionAsync(DispensacionCreateDTO dto)
    {
        logger.LogInformation("Iniciando proceso de dispensación para receta {CodigoReceta}, Medicamento {MedicamentoId}, Cantidad {Cantidad}...",
            dto.CodigoReceta, dto.MedicamentoId, dto.Cantidad);

        // 1. Validación de receta contra servicio hospitalario
        var validacion = await hospitalApiClient.ValidarRecetaAsync(dto.CodigoReceta);
        if (!validacion.EsValida)
        {
            logger.LogWarning("Dispensación rechazada: la receta '{CodigoReceta}' no es válida. Motivo: {Mensaje}",
                dto.CodigoReceta, validacion.Mensaje);
            throw new RecetaInvalidaException(validacion.Mensaje);
        }

        // 2. Transacción atómica de base de datos
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var medicamento = await context.Medicamentos.FirstOrDefaultAsync(m => m.Id == dto.MedicamentoId);
            if (medicamento is null)
            {
                logger.LogWarning("Medicamento con ID {MedicamentoId} no encontrado.", dto.MedicamentoId);
                throw new KeyNotFoundException($"El medicamento con ID {dto.MedicamentoId} no existe en el catálogo.");
            }

            if (medicamento.StockDisponible < dto.Cantidad)
            {
                logger.LogWarning("Conflicto de stock para medicamento '{Nombre}'. Disponible: {Disponible}, Solicitado: {Solicitado}.",
                    medicamento.Nombre, medicamento.StockDisponible, dto.Cantidad);
                throw new StockInsuficienteException(
                    $"Stock insuficiente para el medicamento '{medicamento.Nombre}'. Stock disponible: {medicamento.StockDisponible}, solicitado: {dto.Cantidad}.");
            }

            // Descuento atómico de stock
            medicamento.StockDisponible -= dto.Cantidad;

            var dispensacion = new Dispensacion
            {
                CodigoReceta = dto.CodigoReceta.Trim(),
                MedicamentoId = dto.MedicamentoId,
                Cantidad = dto.Cantidad,
                FechaDispensacion = DateTime.UtcNow
            };

            context.Dispensaciones.Add(dispensacion);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            logger.LogInformation("Dispensación {Id} registrada con éxito para receta {CodigoReceta}. Stock restante de '{Nombre}': {StockRestante}.",
                dispensacion.Id, dispensacion.CodigoReceta, medicamento.Nombre, medicamento.StockDisponible);

            return new DispensacionReadDTO
            {
                Id = dispensacion.Id,
                CodigoReceta = dispensacion.CodigoReceta,
                MedicamentoId = dispensacion.MedicamentoId,
                NombreMedicamento = medicamento.Nombre,
                Cantidad = dispensacion.Cantidad,
                FechaDispensacion = dispensacion.FechaDispensacion
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<DispensacionReadDTO?> GetByIdAsync(int id)
    {
        return await context.Dispensaciones
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DispensacionReadDTO
            {
                Id = d.Id,
                CodigoReceta = d.CodigoReceta,
                MedicamentoId = d.MedicamentoId,
                NombreMedicamento = d.Medicamento != null ? d.Medicamento.Nombre : string.Empty,
                Cantidad = d.Cantidad,
                FechaDispensacion = d.FechaDispensacion
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DispensacionReadDTO>> GetAllAsync()
    {
        return await context.Dispensaciones
            .AsNoTracking()
            .OrderByDescending(d => d.FechaDispensacion)
            .Select(d => new DispensacionReadDTO
            {
                Id = d.Id,
                CodigoReceta = d.CodigoReceta,
                MedicamentoId = d.MedicamentoId,
                NombreMedicamento = d.Medicamento != null ? d.Medicamento.Nombre : string.Empty,
                Cantidad = d.Cantidad,
                FechaDispensacion = d.FechaDispensacion
            })
            .ToListAsync();
    }
}
