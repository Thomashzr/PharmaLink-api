# Seguridad, Pipeline, Errores y Logging --- PharmaLink

Este documento especifica la estrategia de seguridad híbrida (JWT Bearer + API Key), el orden crítico del pipeline de middlewares, el esquema de manejo de excepciones y los estándares de observabilidad y logging estructurado.

---

## 1. Estrategia de Seguridad Híbrida

PharmaLink implementa un esquema de seguridad de dos niveles:
1. **JWT Bearer (Autenticación de Usuarios)**: Para clientes interactivos (Swagger UI, Frontend web/móvil, operadores de farmacia y administradores).
2. **API Key (`X-API-Key`)**: Para integraciones máquina a máquina (intercambio automatizado de datos con el hospital u otros subsistemas).

```
                            PIPELINE DE SEGURIDAD
                            
       Petición HTTP entrante con Headers
                     │
                     ▼
         ┌───────────────────────┐
         │  ExceptionMiddleware  │ Atrapa errores 500 no controlados
         └───────────┬───────────┘
                     ▼
         ┌───────────────────────┐
         │  UseHttpsRedirection  │ Fuerza transporte TLS seguro
         └───────────┬───────────┘
                     ▼
         ┌───────────────────────┐
         │   ApiKeyMiddleware    │ Valida X-API-Key en rutas específicas
         └───────────┬───────────┘
                     ▼
         ┌───────────────────────┐
         │  UseAuthentication    │ Valida firma criptográfica de JWT
         └───────────┬───────────┘
                     ▼
         ┌───────────────────────┐
         │   UseAuthorization    │ Verifica Claims y Roles (Admin/Usuario)
         └───────────┬───────────┘
                     ▼
             Controlador destino
```

---

## 2. Autenticación JWT Bearer

### 2.1 Hashing Seguro de Contraseñas
Las contraseñas nunca se guardan en texto plano. Se utiliza `PasswordHasher<Usuario>` de ASP.NET Core, el cual aplica PBKDF2 con sal criptográfica (*Salt*) aleatoria por usuario y factor de iteración seguro:

```csharp
var hasher = new PasswordHasher<Usuario>();
// Hashear:
usuario.PasswordHash = hasher.HashPassword(usuario, passwordPlano);

// Verificar:
var resultado = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, passwordPlano);
if (resultado == PasswordVerificationResult.Failed) { /* Rechazar */ }
```

### 2.2 Generación y Validación de Tokens
El token JWT incluye los claims esenciales: `NameIdentifier` (ID de usuario), `Name` (Nombre de usuario) y `Role` (Rol asignado).

En `Program.cs`:
```csharp
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtSection["Key"];
if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
{
    throw new InvalidOperationException("La clave JWT_SECRET_KEY debe tener al menos 32 caracteres.");
}

var keyBytes = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Solo en desarrollo local
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.Zero
    };
});
```

---

## 3. Middleware de API Key (`ApiKeyMiddleware`)

Para endpoints de integración directa entre sistemas:
```csharp
namespace PharmaLink.Middleware;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    private const string ApiKeyHeaderName = "X-API-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        // Solo aplica si la ruta requiere explícitamente API Key o si viene el header
        if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            var configuredApiKey = Environment.GetEnvironmentVariable("API_KEY_SECRET") 
                                  ?? config["ApiKey:Secret"];

            if (string.IsNullOrEmpty(configuredApiKey) || 
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(extractedApiKey.ToString()), 
                    Encoding.UTF8.GetBytes(configuredApiKey)))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"mensaje\": \"API Key inválida o no autorizada.\"}");
                return;
            }
        }

        await next(context);
    }
}
```

---

## 4. Gestión de Secretos y Configuración

- **`appsettings.json`**: Contiene configuraciones públicas (nombres de emisor, audiencias, expiración en minutos, connection string de SQLite):
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Data Source=pharmalink.db"
    },
    "Jwt": {
      "Issuer": "PharmaLinkApi",
      "Audience": "PharmaLinkClients",
      "ExpiraMinutos": 60
    },
    "HospitalApi": {
      "BaseUrl": "https://api-hospital.ejemplo.local/v1"
    }
  }
  ```
- **`.env`** (ignorado en `.gitignore`):
  ```env
  JWT_SECRET_KEY=PharmaLink_Super_Clave_Secreta_JWT_2026_Segura_Y_Extensa_12345!
  API_KEY_SECRET=PharmaLink_External_Integration_Key_ABC987654321
  ```
- Se carga en la primera línea de `Program.cs`:
  ```csharp
  DotNetEnv.Env.Load();
  ```

---

## 5. Estrategia de Manejo de Errores en Tres Capas

1. **Capa 1: Entrada y Validación de Modelo**:
   - `[ApiController]` y Data Annotations capturan sintaxis incorrecta o campos faltantes, respondiendo automáticamente con `400 Bad Request` antes de invocar la acción.
2. **Capa 2: Negocio y Dominio**:
   - El controlador y los servicios detectan situaciones esperadas:
     - Recurso no encontrado -> `404 Not Found` (`NotFound(new { mensaje = "..." })`).
     - Stock insuficiente o receta no activa -> `409 Conflict` (`Conflict(new { mensaje = "..." })`).
3. **Capa 3: Global / Infraestructura (`ExceptionMiddleware`)**:
   - Atrapa cualquier excepción no controlada (`NullReferenceException`, falla de disco, desconexión de BD).
   - Escribe el error con detalle completo en `ILogger`.
   - Devuelve una respuesta JSON segura sin filtrar *stack traces* ni datos internos al cliente:
   ```json
   {
     "mensaje": "Ha ocurrido un error interno en el servidor. Por favor contacte al administrador.",
     "traceId": "00-5e347890b..."
   }
   ```

---

## 6. Logging Estructurado con `ILogger<T>`

Reglas de observabilidad:
- **PROHIBIDO** el uso de `Console.WriteLine` en servicios, controladores o middlewares.
- Usar siempre plantillas de mensaje estructurado con parámetros con nombre:
  ```csharp
  // Correcto:
  _logger.LogInformation("Dispensación {Id} completada. Medicamento: {MedicamentoId}, Cantidad: {Cantidad}",
      dispensacion.Id, dispensacion.MedicamentoId, dispensacion.Cantidad);

  _logger.LogWarning("Intento de dispensación rechazado por stock insuficiente para Medicamento {MedicamentoId}",
      medicamentoId);

  _logger.LogError(ex, "Error al contactar la API externa del hospital para la receta {CodigoReceta}",
      codigoReceta);
  ```
- **Seguridad en logs**: No registrar contraseñas, tokens JWT completos, claves API ni diagnósticos médicos sensibles.
