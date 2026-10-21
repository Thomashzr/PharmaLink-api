using System.Security.Cryptography;
using System.Text;

namespace PharmaLink.Middleware;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
{
    private const string ApiKeyHeaderName = "X-API-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var hasApiKeyHeader = context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey);
        var isExternalRoute = path.StartsWith("/api/external", StringComparison.OrdinalIgnoreCase);

        // Si es una ruta dedicada de integración externa o si el cliente envió explícitamente la cabecera X-API-Key
        if (isExternalRoute || hasApiKeyHeader)
        {
            if (!hasApiKeyHeader || string.IsNullOrWhiteSpace(extractedApiKey))
            {
                logger.LogWarning("Intento de acceso no autorizado a {Path}: cabecera {Header} ausente.", path, ApiKeyHeaderName);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"mensaje\": \"Acceso no autorizado: la cabecera X-API-Key es obligatoria para esta operación.\"}");
                return;
            }

            var expectedApiKey = Environment.GetEnvironmentVariable("API_KEY_SECRET")
                                 ?? configuration["Security:ApiKeySecret"]
                                 ?? "PharmaLink_HospitalApiKey_Secret_2026_SecureKey";

            var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
            var extractedBytes = Encoding.UTF8.GetBytes(extractedApiKey.ToString());

            if (expectedBytes.Length != extractedBytes.Length ||
                !CryptographicOperations.FixedTimeEquals(expectedBytes, extractedBytes))
            {
                logger.LogWarning("Intento de acceso no autorizado a {Path}: API Key inválida.", path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"mensaje\": \"Acceso no autorizado: la clave provista en X-API-Key es inválida.\"}");
                return;
            }

            logger.LogInformation("Autenticación por API Key validada exitosamente para {Path}.", path);
        }

        await next(context);
    }
}
