using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PharmaLink.ExternalServices;

namespace PharmaLink.Tests.Unit;

public class HospitalApiClientTests
{
    private readonly Mock<ILogger<HospitalApiClient>> _loggerMock = new();

    private static IConfiguration CreateRealModeConfig(string apiKey = "compilaDeMilagro123")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HospitalApi:BaseUrl"] = "http://localhost:5144",
                ["HospitalApi:ApiKey"] = apiKey,
                ["HospitalApi:SimulatedMode"] = "false"
            })
            .Build();
    }

    [Fact]
    public async Task ValidarRecetaAsync_ModoReal200Ok_UsaPostEndpointCorrectoHeaderApiKeyYMapeaApiResponse()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var jsonResponse = """
        {
            "success": true,
            "message": "Receta válida para dispensar.",
            "data": {
                "codigo": "RX-1A2B3C4D",
                "medicamentoId": 4,
                "cantidad": 2
            }
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5144")
        };

        var client = new HospitalApiClient(httpClient, CreateRealModeConfig(), _loggerMock.Object);

        // Act
        var result = await client.ValidarRecetaAsync("RX-1A2B3C4D");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("/api/farmacia/recetas/RX-1A2B3C4D/validar", capturedRequest.RequestUri!.AbsolutePath);
        Assert.True(capturedRequest.Headers.TryGetValues("X-Api-Key", out var headerValues));
        Assert.Contains("compilaDeMilagro123", headerValues!);

        Assert.True(result.EsValida);
        Assert.Equal("RX-1A2B3C4D", result.CodigoReceta);
        Assert.Equal(4, result.MedicamentoId);
        Assert.Equal(2, result.CantidadPrescripta);
        Assert.Equal("Receta válida para dispensar.", result.Mensaje);
    }

    [Fact]
    public async Task ValidarRecetaAsync_ModoReal404NotFound_DevuelveEsValidaFalseConMensajeDeMediLink()
    {
        // Arrange
        var jsonResponse = """
        {
            "success": false,
            "message": "La receta RX-NOEXISTE no fue encontrada.",
            "data": null
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5144")
        };

        var client = new HospitalApiClient(httpClient, CreateRealModeConfig(), _loggerMock.Object);

        // Act
        var result = await client.ValidarRecetaAsync("RX-NOEXISTE");

        // Assert
        Assert.False(result.EsValida);
        Assert.Equal("RX-NOEXISTE", result.CodigoReceta);
        Assert.Equal("La receta RX-NOEXISTE no fue encontrada.", result.Mensaje);
    }

    [Fact]
    public async Task DispensarRecetaAsync_ModoReal200OkY409Conflict_UsaPostDispensarYManejaConflicto()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var conflictJson = """
        {
            "success": false,
            "message": "La receta ya fue dispensada anteriormente.",
            "data": null
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Conflict,
                Content = new StringContent(conflictJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5144")
        };

        var client = new HospitalApiClient(httpClient, CreateRealModeConfig(), _loggerMock.Object);

        // Act
        var result = await client.DispensarRecetaAsync("RX-USADA");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("/api/farmacia/recetas/RX-USADA/dispensar", capturedRequest.RequestUri!.AbsolutePath);
        Assert.False(result.EsValida);
        Assert.Equal("La receta ya fue dispensada anteriormente.", result.Mensaje);
    }
}
