using Microsoft.Extensions.Logging;
using Moq;
using PharmaLink.DTOs.Medicamentos;
using PharmaLink.Models;
using PharmaLink.Services;
using PharmaLink.Tests.Helpers;

namespace PharmaLink.Tests.Unit;

public class MedicamentoDbServiceTests
{
    private readonly Mock<ILogger<MedicamentoDbService>> _loggerMock = new();

    [Fact]
    public async Task GetAllAsync_ConFiltro_DebeRetornarSoloCoincidentes()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            context.Medicamentos.AddRange(
                new Medicamento { Nombre = "Paracetamol 500mg", Presentacion = "Comprimidos", StockDisponible = 50, PrecioUnitario = 100m },
                new Medicamento { Nombre = "Ibuprofeno 400mg", Presentacion = "Cápsulas", StockDisponible = 30, PrecioUnitario = 200m }
            );
            await context.SaveChangesAsync();

            var service = new MedicamentoDbService(context, _loggerMock.Object);

            // Act
            var result = (await service.GetAllAsync("ibu")).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("Ibuprofeno 400mg", result[0].Nombre);
        }
    }

    [Fact]
    public async Task DeleteAsync_SinDispensaciones_DebeEliminarCorrectamente()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var med = new Medicamento { Nombre = "Aspirina 100mg", Presentacion = "Comprimidos", StockDisponible = 20, PrecioUnitario = 80m };
            context.Medicamentos.Add(med);
            await context.SaveChangesAsync();

            var service = new MedicamentoDbService(context, _loggerMock.Object);

            // Act
            var eliminado = await service.DeleteAsync(med.Id);

            // Assert
            Assert.True(eliminado);
            var busqueda = await context.Medicamentos.FindAsync(med.Id);
            Assert.Null(busqueda);
        }
    }

    [Fact]
    public async Task DeleteAsync_ConDispensacionesAsociadas_DebeLanzarInvalidOperationException()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var med = new Medicamento { Nombre = "Amoxicilina 500mg", Presentacion = "Comprimidos", StockDisponible = 40, PrecioUnitario = 350m };
            context.Medicamentos.Add(med);
            await context.SaveChangesAsync();

            var disp = new Dispensacion { CodigoReceta = "REC-HIST-01", MedicamentoId = med.Id, Cantidad = 2, FechaDispensacion = DateTime.UtcNow };
            context.Dispensaciones.Add(disp);
            await context.SaveChangesAsync();

            var service = new MedicamentoDbService(context, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(med.Id));
        }
    }
}
