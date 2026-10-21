using System.Net;
using System.Text.Json;

namespace PharmaLink.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excepción no controlada capturada en middleware para {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var respuesta = new
        {
            status = context.Response.StatusCode,
            title = "Error interno del servidor",
            detail = env.IsDevelopment()
                ? exception.Message
                : "Ha ocurrido un error inesperado al procesar la solicitud. Por favor intente más tarde."
        };

        var json = JsonSerializer.Serialize(respuesta);
        return context.Response.WriteAsync(json);
    }
}
