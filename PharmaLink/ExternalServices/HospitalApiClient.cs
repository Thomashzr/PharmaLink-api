using System.Net;
using System.Net.Http.Json;
using PharmaLink.DTOs.External;
using PharmaLink.Interfaces;

namespace PharmaLink.ExternalServices;

public class HospitalApiClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HospitalApiClient> logger
) : IHospitalApiClient
{
    public async Task<RecetaValidacionResultDTO> ValidarRecetaAsync(string codigoReceta)
    {
        if (string.IsNullOrWhiteSpace(codigoReceta))
        {
            return new RecetaValidacionResultDTO
            {
                EsValida = false,
                CodigoReceta = codigoReceta ?? string.Empty,
                Mensaje = "El código de receta no puede estar vacío."
            };
        }

        var simulatedEnv = Environment.GetEnvironmentVariable("HOSPITAL_API_SIMULATED_MODE");
        var isSimulated = !string.IsNullOrEmpty(simulatedEnv)
            ? bool.Parse(simulatedEnv)
            : configuration.GetValue<bool>("HospitalApi:SimulatedMode", true);

        if (isSimulated)
        {
            logger.LogInformation("Validando receta {CodigoReceta} en modo SIMULADO.", codigoReceta);

            var esInvalida = codigoReceta.Contains("ERR", StringComparison.OrdinalIgnoreCase) ||
                             codigoReceta.Contains("INVALID", StringComparison.OrdinalIgnoreCase) ||
                             codigoReceta.Contains("RECHAZADA", StringComparison.OrdinalIgnoreCase);

            if (esInvalida)
            {
                logger.LogWarning("Receta {CodigoReceta} rechazada en modo simulado.", codigoReceta);
                return new RecetaValidacionResultDTO
                {
                    EsValida = false,
                    CodigoReceta = codigoReceta,
                    Mensaje = $"La receta '{codigoReceta}' no se encuentra activa o no fue autorizada por el sistema hospitalario."
                };
            }

            logger.LogInformation("Receta {CodigoReceta} validada exitosamente en modo simulado.", codigoReceta);
            return new RecetaValidacionResultDTO
            {
                EsValida = true,
                CodigoReceta = codigoReceta,
                Mensaje = "Receta validada y autorizada con éxito por el hospital."
            };
        }

        // Modo real contra API externa del Hospital
        try
        {
            logger.LogInformation("Consultando API externa del hospital para la receta {CodigoReceta}...", codigoReceta);

            var endpoint = $"/api/recetas/{Uri.EscapeDataString(codigoReceta)}";
            var response = await httpClient.GetAsync(endpoint);

            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                logger.LogWarning("La receta {CodigoReceta} no fue encontrada o es inválida en la API hospitalaria (HTTP {StatusCode}).", codigoReceta, (int)response.StatusCode);
                return new RecetaValidacionResultDTO
                {
                    EsValida = false,
                    CodigoReceta = codigoReceta,
                    Mensaje = $"La receta '{codigoReceta}' no es válida según el hospital."
                };
            }

            response.EnsureSuccessStatusCode();

            var resultado = await response.Content.ReadFromJsonAsync<RecetaValidacionResultDTO>();
            return resultado ?? new RecetaValidacionResultDTO
            {
                EsValida = false,
                CodigoReceta = codigoReceta,
                Mensaje = "Respuesta vacía recibida desde la API hospitalaria."
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error de conectividad HTTP al contactar con la API hospitalaria externa.");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout al consultar la API hospitalaria externa.");
            throw new HttpRequestException("La API hospitalaria externa no respondió a tiempo (Timeout).", ex, HttpStatusCode.GatewayTimeout);
        }
    }
}
