using Microsoft.Extensions.Logging;
using Moq;
using PharmaLink.DTOs.Reposiciones;
using PharmaLink.Models;
using PharmaLink.Services;
using PharmaLink.Tests.Helpers;

namespace PharmaLink.Tests.Unit;

public class ReposicionDbServiceTests
{
    private readonly Mock<ILogger<ReposicionDbService>> _loggerMock = new();

    [Fact]
    public async Task CrearSolicitudAsync_DebeCrearEnEstadoSolicitada_YNoModificarStock()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var insumo = new Insumo { Nombre = "Guantes M", Presentacion = "Caja x 100", StockDisponible = 10, PrecioUnitario = 1000m };
            context.Insumos.Add(insumo);
            await context.SaveChangesAsync();

            var service = new ReposicionDbService(context, _loggerMock.Object);
            var dto = new ReposicionCreateDTO
            {
                Items = new List<EntregaItemDTO>
                {
                    new() { InsumoId = insumo.Id, Cantidad = 25 }
                }
            };

            // Act
            var result = await service.CrearSolicitudAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Solicitada", result.Estado);
            Assert.Single(result.Entregas);

            var insumoDb = await context.Insumos.FindAsync(insumo.Id);
            Assert.Equal(10, insumoDb!.StockDisponible); // Stock no alterado en la solicitud
        }
    }

    [Fact]
    public async Task ConfirmarReposicionAsync_EstadoSolicitada_DebePasarAConfirmadaEIncrementarStock()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var insumo = new Insumo { Nombre = "Jeringas 5ml", Presentacion = "Caja x 50", StockDisponible = 15, PrecioUnitario = 800m };
            context.Insumos.Add(insumo);
            await context.SaveChangesAsync();

            var service = new ReposicionDbService(context, _loggerMock.Object);
            var createDto = new ReposicionCreateDTO
            {
                Items = new List<EntregaItemDTO>
                {
                    new() { InsumoId = insumo.Id, Cantidad = 30 }
                }
            };
            var creada = await service.CrearSolicitudAsync(createDto);

            // Act
            var confirmada = await service.ConfirmarReposicionAsync(creada.Id);

            // Assert
            Assert.Equal("Confirmada", confirmada.Estado);
            Assert.NotNull(confirmada.FechaConfirmacion);

            var insumoDb = await context.Insumos.FindAsync(insumo.Id);
            Assert.Equal(45, insumoDb!.StockDisponible); // 15 + 30 = 45
        }
    }

    [Fact]
    public async Task ConfirmarReposicionAsync_YaConfirmada_DebeLanzarInvalidOperationException()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var insumo = new Insumo { Nombre = "Gasas 10x10", Presentacion = "Sobre x 10", StockDisponible = 20, PrecioUnitario = 300m };
            context.Insumos.Add(insumo);
            await context.SaveChangesAsync();

            var service = new ReposicionDbService(context, _loggerMock.Object);
            var createDto = new ReposicionCreateDTO
            {
                Items = new List<EntregaItemDTO>
                {
                    new() { InsumoId = insumo.Id, Cantidad = 10 }
                }
            };
            var creada = await service.CrearSolicitudAsync(createDto);
            await service.ConfirmarReposicionAsync(creada.Id);

            // Act & Assert (segundo intento debe fallar para evitar doble stock)
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarReposicionAsync(creada.Id));

            var insumoDb = await context.Insumos.FindAsync(insumo.Id);
            Assert.Equal(30, insumoDb!.StockDisponible); // No suma otra vez
        }
    }

    [Fact]
    public async Task ObtenerResumenAsync_DebeCalcularMetricasYStockActual()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var insumo1 = new Insumo { Nombre = "Insumo A", Presentacion = "Unidad", StockDisponible = 10, PrecioUnitario = 100m };
            var insumo2 = new Insumo { Nombre = "Insumo B", Presentacion = "Unidad", StockDisponible = 20, PrecioUnitario = 200m };
            context.Insumos.AddRange(insumo1, insumo2);
            await context.SaveChangesAsync();

            var service = new ReposicionDbService(context, _loggerMock.Object);
            var rep1 = await service.CrearSolicitudAsync(new ReposicionCreateDTO
            {
                Items = new List<EntregaItemDTO> { new() { InsumoId = insumo1.Id, Cantidad = 5 } }
            });
            await service.ConfirmarReposicionAsync(rep1.Id);

            await service.CrearSolicitudAsync(new ReposicionCreateDTO
            {
                Items = new List<EntregaItemDTO> { new() { InsumoId = insumo2.Id, Cantidad = 10 } }
            });

            // Act
            var resumen = await service.ObtenerResumenAsync();

            // Assert
            Assert.Equal(2, resumen.TotalSolicitudes);
            Assert.Equal(1, resumen.TotalConfirmadas);
            Assert.Equal(1, resumen.TotalPendientes);
            Assert.Equal(2, resumen.StockInsumos.Count);
        }
    }
}
