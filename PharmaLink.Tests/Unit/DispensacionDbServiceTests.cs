using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using PharmaLink.DTOs.Dispensaciones;
using PharmaLink.DTOs.External;
using PharmaLink.Exceptions;
using PharmaLink.Interfaces;
using PharmaLink.Models;
using PharmaLink.Services;
using PharmaLink.Tests.Helpers;

namespace PharmaLink.Tests.Unit;

public class DispensacionDbServiceTests
{
    private readonly Mock<IHospitalApiClient> _hospitalClientMock = new();
    private readonly Mock<ILogger<DispensacionDbService>> _loggerMock = new();

    [Fact]
    public async Task RegistrarDispensacionAsync_RecetaValidaYStockSuficiente_DebeDecrementarStockYRegistrar()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var med = new Medicamento { Nombre = "Paracetamol 500mg", Presentacion = "Caja x 20", StockDisponible = 10, PrecioUnitario = 500m };
            context.Medicamentos.Add(med);
            await context.SaveChangesAsync();

            _hospitalClientMock.Setup(h => h.ValidarRecetaAsync("REC-OK-01"))
                .ReturnsAsync(new RecetaValidacionResultDTO { EsValida = true, CodigoReceta = "REC-OK-01" });

            var service = new DispensacionDbService(context, _hospitalClientMock.Object, _loggerMock.Object);
            var dto = new DispensacionCreateDTO { CodigoReceta = "REC-OK-01", MedicamentoId = med.Id, Cantidad = 3 };

            // Act
            var result = await service.RegistrarDispensacionAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("REC-OK-01", result.CodigoReceta);
            Assert.Equal(3, result.Cantidad);

            var medActualizado = await context.Medicamentos.FindAsync(med.Id);
            Assert.Equal(7, medActualizado!.StockDisponible); // 10 - 3 = 7
        }
    }

    [Fact]
    public async Task RegistrarDispensacionAsync_RecetaInvalida_DebeLanzarRecetaInvalidaExceptionYNoAlterarStock()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var med = new Medicamento { Nombre = "Paracetamol 500mg", Presentacion = "Caja x 20", StockDisponible = 10, PrecioUnitario = 500m };
            context.Medicamentos.Add(med);
            await context.SaveChangesAsync();

            _hospitalClientMock.Setup(h => h.ValidarRecetaAsync("REC-ERR-01"))
                .ReturnsAsync(new RecetaValidacionResultDTO { EsValida = false, CodigoReceta = "REC-ERR-01", Mensaje = "Receta no activa" });

            var service = new DispensacionDbService(context, _hospitalClientMock.Object, _loggerMock.Object);
            var dto = new DispensacionCreateDTO { CodigoReceta = "REC-ERR-01", MedicamentoId = med.Id, Cantidad = 3 };

            // Act & Assert
            await Assert.ThrowsAsync<RecetaInvalidaException>(() => service.RegistrarDispensacionAsync(dto));

            var medActualizado = await context.Medicamentos.FindAsync(med.Id);
            Assert.Equal(10, medActualizado!.StockDisponible); // Stock intacto
        }
    }

    [Fact]
    public async Task RegistrarDispensacionAsync_StockInsuficiente_DebeLanzarStockInsuficienteException()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var med = new Medicamento { Nombre = "Paracetamol 500mg", Presentacion = "Caja x 20", StockDisponible = 2, PrecioUnitario = 500m };
            context.Medicamentos.Add(med);
            await context.SaveChangesAsync();

            _hospitalClientMock.Setup(h => h.ValidarRecetaAsync("REC-OK-02"))
                .ReturnsAsync(new RecetaValidacionResultDTO { EsValida = true, CodigoReceta = "REC-OK-02" });

            var service = new DispensacionDbService(context, _hospitalClientMock.Object, _loggerMock.Object);
            var dto = new DispensacionCreateDTO { CodigoReceta = "REC-OK-02", MedicamentoId = med.Id, Cantidad = 5 }; // Solicita 5, hay 2

            // Act & Assert
            await Assert.ThrowsAsync<StockInsuficienteException>(() => service.RegistrarDispensacionAsync(dto));

            var medActualizado = await context.Medicamentos.FindAsync(med.Id);
            Assert.Equal(2, medActualizado!.StockDisponible); // No decrementa
        }
    }

    [Fact]
    public async Task RegistrarDispensacionAsync_ErrorConectividadHospital_DebePropagarHttpRequestException()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var med = new Medicamento { Nombre = "Paracetamol 500mg", Presentacion = "Caja x 20", StockDisponible = 10, PrecioUnitario = 500m };
            context.Medicamentos.Add(med);
            await context.SaveChangesAsync();

            _hospitalClientMock.Setup(h => h.ValidarRecetaAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Error de red", null, HttpStatusCode.BadGateway));

            var service = new DispensacionDbService(context, _hospitalClientMock.Object, _loggerMock.Object);
            var dto = new DispensacionCreateDTO { CodigoReceta = "REC-TIMEOUT", MedicamentoId = med.Id, Cantidad = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => service.RegistrarDispensacionAsync(dto));
        }
    }
}
