# Persistencia e Integración Externa --- PharmaLink

Este documento especifica la capa de persistencia con Entity Framework Core sobre SQLite, el manejo transaccional de concurrencia y la integración aislada con el servicio hospitalario externo.

---

## 1. Persistencia con Entity Framework Core y SQLite

### 1.1 Paquetes NuGet Requeridos
Alinear todos los paquetes en la versión LTS instalada (.NET 8 o .NET 9):
```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package DotNetEnv
```

Herramienta global para migraciones:
```bash
dotnet tool install --global dotnet-ef
```

### 1.2 Configuración del `DbContext`
El contexto reside en `Context/PharmaLinkContext.cs`. No se crea una carpeta `Data/` separada ni clases de repositorio intermedias.

En `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=pharmalink.db"
  }
}
```

Registro en `Program.cs`:
```csharp
builder.Services.AddDbContext<PharmaLinkContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### 1.3 Ciclo de Vida de las Migraciones
```bash
# 1. Crear una migración inicial
dotnet ef migrations add MigracionInicial

# 2. Aplicar la migración a la base de datos (crea o actualiza pharmalink.db)
dotnet ef database update

# 3. Listar migraciones aplicadas y pendientes
dotnet ef migrations list
```

---

## 2. Consistencia Transaccional y Control de Concurrencia de Stock

### 2.1 El Problema de Concurrencia
Si dos solicitudes concurrentes intentan dispensar el mismo medicamento cuando el stock disponible es 1:
- Ambas leen `StockDisponible == 1`.
- Ambas aprueban la condición.
- Ambas descuentan 1 y guardan, dejando el stock en `-1` o generando una doble dispensación indebida.

### 2.2 Estrategia de Solución Transaccional en `DispensacionDbService`
La operación se envuelve en una transacción de base de datos (`IDbContextTransaction`) con bloqueo o validación de balance:

```csharp
public async Task<DispensacionReadDTO?> RegistrarDispensacionAsync(DispensacionCreateDTO dto)
{
    // 1. Validar receta con API externa primero (fuera de la transacción de BD)
    var recetaValida = await _hospitalApiClient.ValidarRecetaAsync(dto.CodigoReceta, dto.MedicamentoId, dto.Cantidad);
    if (!recetaValida)
    {
        throw new InvalidOperationException("La receta médica no es válida, no está activa o no coincide con el medicamento.");
    }

    // 2. Iniciar transacción atómica local
    await using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 3. Obtener el medicamento con tracking activo
        var medicamento = await _context.Medicamentos.FirstOrDefaultAsync(m => m.Id == dto.MedicamentoId);
        if (medicamento == null)
        {
            throw new KeyNotFoundException($"El medicamento con ID {dto.MedicamentoId} no existe.");
        }

        // 4. Validar stock disponible
        if (medicamento.StockDisponible < dto.Cantidad)
        {
            throw new InvalidOperationException($"Stock insuficiente. Disponible: {medicamento.StockDisponible}, Solicitado: {dto.Cantidad}.");
        }

        // 5. Decrementar stock
        medicamento.StockDisponible -= dto.Cantidad;

        // 6. Registrar la entidad de dispensación
        var dispensacion = new Dispensacion
        {
            CodigoReceta = dto.CodigoReceta,
            MedicamentoId = dto.MedicamentoId,
            Cantidad = dto.Cantidad,
            FechaDispensacion = DateTime.UtcNow
        };
        _context.Dispensaciones.Add(dispensacion);

        // 7. Persistir y confirmar la transacción
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Dispensación {Id} confirmada. Stock actualizado a {Stock}", dispensacion.Id, medicamento.StockDisponible);

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
```

---

## 3. Integración con la API Externa del Hospital

### 3.1 Contrato de la API Externa (`Interfaces/IHospitalApiClient.cs`)
```csharp
namespace PharmaLink.Interfaces;

public interface IHospitalApiClient
{
    Task<bool> ValidarRecetaAsync(string codigoReceta, int medicamentoId, int cantidad);
}
```

### 3.2 Implementación del Cliente (`ExternalServices/HospitalApiClient.cs`)
```csharp
namespace PharmaLink.ExternalServices;

public class HospitalApiClient(HttpClient httpClient, ILogger<HospitalApiClient> logger) : IHospitalApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<HospitalApiClient> _logger = logger;

    public async Task<bool> ValidarRecetaAsync(string codigoReceta, int medicamentoId, int cantidad)
    {
        try
        {
            var uri = $"/api/recetas/{Uri.EscapeDataString(codigoReceta)}/validar";
            var payload = new { MedicamentoId = medicamentoId, Cantidad = cantidad };

            var response = await _httpClient.PostAsJsonAsync(uri, payload);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Receta {CodigoReceta} rechazada por el hospital (Status: {StatusCode})", 
                    codigoReceta, response.StatusCode);
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Fallo de infraestructura en API Hospital. StatusCode: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Fallo al comunicarse con el hospital. Código HTTP: {response.StatusCode}");
            }

            var resultado = await response.Content.ReadFromJsonAsync<RespuestaRecetaHospitalDTO>();
            return resultado?.EsValida ?? false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout al comunicarse con la API del hospital para receta {CodigoReceta}", codigoReceta);
            throw new HttpRequestException("Timeout en la conexión con el servicio del hospital", ex);
        }
    }

    private record RespuestaRecetaHospitalDTO(bool EsValida, string? Mensaje);
}
```

### 3.3 Aislamiento de Errores de Integración
- **Receta rechazada (lógica de negocio)**: La API externa responde que la receta expiró o no existe. El servicio local captura esto y responde `400 Bad Request` o `409 Conflict`.
- **Falla técnica del hospital (indisponibilidad o timeout)**: Se propaga una `HttpRequestException` que el middleware traduce a **`502 Bad Gateway`** con un mensaje claro al usuario: *"El servicio de validación de recetas del hospital no se encuentra disponible momentáneamente. Por favor reintente más tarde."*
- **Privacidad y GDPR/HIPAA**: No registrar datos de salud ni nombres de pacientes en logs locales.
