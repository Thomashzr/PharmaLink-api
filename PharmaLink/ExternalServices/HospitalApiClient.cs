using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PharmaLink.DTOs.External;
using PharmaLink.Interfaces;

namespace PharmaLink.ExternalServices;

public class HospitalApiClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HospitalApiClient> logger
) : IHospitalApiClient
{
    private const string ApiKeyHeaderName = "X-Api-Key";

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

        if (IsSimulatedMode())
        {
            logger.LogInformation("Validando receta {CodigoReceta} en modo SIMULADO.", codigoReceta);

            if (EsCodigoInvalidoSimulado(codigoReceta))
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

        try
        {
            EnsureApiKeyHeader();
            logger.LogInformation("Validando receta {CodigoReceta} en MediLink...", codigoReceta);

            var endpoint = $"/api/farmacia/recetas/{Uri.EscapeDataString(codigoReceta)}/validar";
            var response = await httpClient.PostAsync(endpoint, content: null);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogError("MediLink rechazó la autenticación (401 Unauthorized) al validar la receta {CodigoReceta}. Verifique la cabecera {Header}.", codigoReceta, ApiKeyHeaderName);
                throw new HttpRequestException("Acceso no autorizado al consultar la API hospitalaria MediLink (X-Api-Key inválida o ausente).", null, HttpStatusCode.Unauthorized);
            }

            var apiResponse = await TryReadMediLinkResponseAsync(response);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity or HttpStatusCode.Conflict)
            {
                var motivo = !string.IsNullOrWhiteSpace(apiResponse?.Message)
                    ? apiResponse.Message
                    : $"La receta '{codigoReceta}' no es válida según el hospital.";

                logger.LogWarning("La receta {CodigoReceta} fue rechazada por MediLink (HTTP {StatusCode}): {Motivo}", codigoReceta, (int)response.StatusCode, motivo);
                return new RecetaValidacionResultDTO
                {
                    EsValida = false,
                    CodigoReceta = codigoReceta,
                    Mensaje = motivo
                };
            }

            response.EnsureSuccessStatusCode();

            if (apiResponse is null)
            {
                return new RecetaValidacionResultDTO
                {
                    EsValida = false,
                    CodigoReceta = codigoReceta,
                    Mensaje = "Respuesta vacía recibida desde la API hospitalaria."
                };
            }

            return MapToResultDto(apiResponse, codigoReceta, "Receta validada y autorizada con éxito por el hospital.");
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

    public async Task<RecetaValidacionResultDTO> DispensarRecetaAsync(string codigoReceta)
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

        if (IsSimulatedMode())
        {
            logger.LogInformation("Marcando receta {CodigoReceta} como dispensada en modo SIMULADO.", codigoReceta);

            if (EsCodigoInvalidoSimulado(codigoReceta) ||
                codigoReceta.Contains("DISPENSADA", StringComparison.OrdinalIgnoreCase) ||
                codigoReceta.Contains("VENCIDA", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Dispensación de receta {CodigoReceta} rechazada en modo simulado.", codigoReceta);
                return new RecetaValidacionResultDTO
                {
                    EsValida = false,
                    CodigoReceta = codigoReceta,
                    Mensaje = $"La receta '{codigoReceta}' ya fue dispensada o se encuentra vencida."
                };
            }

            logger.LogInformation("Receta {CodigoReceta} marcada como dispensada exitosamente en modo simulado.", codigoReceta);
            return new RecetaValidacionResultDTO
            {
                EsValida = true,
                CodigoReceta = codigoReceta,
                Mensaje = "Receta marcada como dispensada con éxito por el hospital."
            };
        }

        try
        {
            EnsureApiKeyHeader();
            logger.LogInformation("Marcando receta {CodigoReceta} como dispensada en MediLink...", codigoReceta);

            var endpoint = $"/api/farmacia/recetas/{Uri.EscapeDataString(codigoReceta)}/dispensar";
            var response = await httpClient.PostAsync(endpoint, content: null);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogError("MediLink rechazó la autenticación (401 Unauthorized) al dispensar la receta {CodigoReceta}. Verifique la cabecera {Header}.", codigoReceta, ApiKeyHeaderName);
                throw new HttpRequestException("Acceso no autorizado al consultar la API hospitalaria MediLink (X-Api-Key inválida o ausente).", null, HttpStatusCode.Unauthorized);
            }

            var apiResponse = await TryReadMediLinkResponseAsync(response);

            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            {
                var motivo = !string.IsNullOrWhiteSpace(apiResponse?.Message)
                    ? apiResponse.Message
                    : $"La receta '{codigoReceta}' no pudo marcarse como dispensada en el hospital.";

                logger.LogWarning("No se pudo marcar como dispensada la receta {CodigoReceta} en MediLink (HTTP {StatusCode}): {Motivo}", codigoReceta, (int)response.StatusCode, motivo);
                return new RecetaValidacionResultDTO
                {
                    EsValida = false,
                    CodigoReceta = codigoReceta,
                    Mensaje = motivo
                };
            }

            response.EnsureSuccessStatusCode();

            if (apiResponse is null)
            {
                return new RecetaValidacionResultDTO
                {
                    EsValida = false,
                    CodigoReceta = codigoReceta,
                    Mensaje = "Respuesta vacía recibida desde la API hospitalaria al dispensar la receta."
                };
            }

            return MapToResultDto(apiResponse, codigoReceta, "Receta marcada como dispensada con éxito por el hospital.");
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

    private bool IsSimulatedMode()
    {
        var simulatedEnv = Environment.GetEnvironmentVariable("HOSPITAL_API_SIMULATED_MODE");
        return !string.IsNullOrEmpty(simulatedEnv)
            ? bool.Parse(simulatedEnv)
            : configuration.GetValue<bool>("HospitalApi:SimulatedMode", true);
    }

    private static bool EsCodigoInvalidoSimulado(string codigoReceta) =>
        codigoReceta.Contains("ERR", StringComparison.OrdinalIgnoreCase) ||
        codigoReceta.Contains("INVALID", StringComparison.OrdinalIgnoreCase) ||
        codigoReceta.Contains("RECHAZADA", StringComparison.OrdinalIgnoreCase);

    private void EnsureApiKeyHeader()
    {
        if (httpClient.DefaultRequestHeaders.Contains(ApiKeyHeaderName))
        {
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable("HOSPITAL_API_KEY")
                     ?? configuration["HospitalApi:ApiKey"]
                     ?? "compilaDeMilagro123";

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);
        }
    }

    private static async Task<MediLinkApiResponse?> TryReadMediLinkResponseAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<MediLinkApiResponse>();
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static RecetaValidacionResultDTO MapToResultDto(MediLinkApiResponse apiResponse, string fallbackCodigo, string defaultMessage)
    {
        var codigo = ExtraerString(apiResponse.Data, "codigo", "codigoReceta") ?? fallbackCodigo;
        var medicamentoId = ExtraerInt(apiResponse.Data, "medicamentoId");
        var cantidadPrescripta = ExtraerInt(apiResponse.Data, "cantidadPrescripta", "cantidad");

        return new RecetaValidacionResultDTO
        {
            EsValida = apiResponse.Success,
            CodigoReceta = codigo,
            MedicamentoId = medicamentoId,
            CantidadPrescripta = cantidadPrescripta,
            Mensaje = !string.IsNullOrWhiteSpace(apiResponse.Message) ? apiResponse.Message : defaultMessage
        };
    }

    private static string? ExtraerString(JsonElement? data, params string[] propertyNames)
    {
        if (data is not { ValueKind: JsonValueKind.Object } element)
        {
            return null;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (propertyNames.Any(name => string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)) &&
                prop.Value.ValueKind == JsonValueKind.String)
            {
                return prop.Value.GetString();
            }
        }

        return null;
    }

    private static int? ExtraerInt(JsonElement? data, params string[] propertyNames)
    {
        if (data is not { ValueKind: JsonValueKind.Object } element)
        {
            return null;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (propertyNames.Any(name => string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var value))
                {
                    return value;
                }

                if (prop.Value.ValueKind == JsonValueKind.String && int.TryParse(prop.Value.GetString(), out var parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }

    private sealed class MediLinkApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public JsonElement? Data { get; set; }
    }
}
