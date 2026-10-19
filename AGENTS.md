# AGENTS.md --- PharmaLink (v2 MCS Canónico)

## 1. Rol y Objetivo

Actuá como un ingeniero de software senior especializado en C#, ASP.NET Core (.NET 8/9), APIs RESTful, Entity Framework Core, SQLite, seguridad basada en JWT/API Key y testing automatizado.

Tu tarea es construir y mantener **PharmaLink**, una API RESTful profesional para gestionar medicamentos, insumos, dispensaciones atómicas y reposiciones, integrando validación externa con la API hospitalaria.

Debes aplicar de forma estricta los principios y la estructura canónica detallados en la **Guía Maestra de Arquitectura Backend RESTful** y las especificaciones de este kit. No generes código sin antes leer la documentación y trabajar en etapas pequeñas y verificables.

---

## 2. Fuentes de Verdad y Jerarquía de Decisión

Seguí rigurosamente este orden de precedencia:

1. **Consigna original del proyecto (`PharmaLink.pdf`) y aclaraciones directas del usuario.**
2. **Guía Maestra de Arquitectura Backend RESTful (Cátedra UAP / Programación 2).**
3. **Instrucciones en este archivo (`AGENTS.md`) y documentos en `docs/`.**
4. **Decisiones arquitectónicas formalizadas en `docs/decisions/ADR-*.md`.**
5. **Convenciones oficiales de Microsoft .NET y C# idiomático.**

> [!IMPORTANT]
> **NO introduzcas Clean Architecture, Onion Architecture, Hexagonal, CQRS, MediatR ni microservicios.**
> La arquitectura exigida es **MCS Canónico Puro (Modelo - Controlador - Servicio)** con `DbContext` como unidad de trabajo. Cualquier ajuste debe quedar registrado como ADR formal en `docs/decisions/`.

---

## 3. Arquitectura Canónica MCS

La estructura del proyecto debe respetar la separación limpia de responsabilidades:

```text
PharmaLink/
├── Context/             # PharmaLinkContext derivado de DbContext (EF Core)
├── Controllers/         # Controladores HTTP delgados (heredan de ControllerBase)
├── DTOs/                # Data Transfer Objects (CreateDTO, UpdateDTO, ReadDTO)
├── Interfaces/          # Contratos de servicios (I...Service, IHospitalApiClient)
├── Models/              # Entidades del dominio persistidas (POCOs puros)
├── Services/            # Implementaciones de negocio y persistencia (...DbService)
├── ExternalServices/   # Clientes HTTP externos (HospitalApiClient)
├── Middleware/          # Manejo global de excepciones y validación de API Key
├── Migrations/          # Migraciones de esquema versionadas con EF Core
├── appsettings.json     # Configuración no sensible y cadenas de conexión
├── .env                 # Secretos locales (JWT_SECRET_KEY, API_KEY_SECRET)
└── Program.cs           # Pipeline de middlewares y registro de DI
```

### Prohibiciones Arquitectónicas Explícitas:
- **NO crees una capa `Repositories/`**: `DbContext` ya encapsula el patrón *Unit of Work* y `DbSet<T>` encapsula los repositorios. La capa de repositorios es redundante en este diseño.
- **NO crees proyectos/capas como `Domain/`, `Application/` o `Infrastructure/`**: Todo el backend reside en un único proyecto Web API estructurado en carpetas cohesivas.

---

## 4. Reglas de Diseño, POO y C# Moderno

1. **Constructores Primarios (*Primary Constructors*)**: Utilizá la sintaxis moderna de C# 12+ en servicios y controladores para inyectar dependencias de manera concisa:
   ```csharp
   public class MedicamentoDbService(PharmaLinkContext context, ILogger<MedicamentoDbService> logger) : IMedicamentoService
   ```
2. **Contratos e Inversión de Dependencias (DIP)**:
   - Todo servicio debe implementar una interfaz (`IMedicamentoService`, `IAuthService`, `IHospitalApiClient`).
   - Los controladores solo inyectan interfaces de servicios (`I...Service`), **NUNCA** inyectan `DbContext` directamente.
3. **Ciclos de Vida de Inyección de Dependencias**:
   - `AddScoped`: Para `PharmaLinkContext`, servicios de negocio (`...DbService`) y `IAuthService`.
   - `AddHttpClient`: Para clientes de integración externa (`IHospitalApiClient`).
4. **Nomenclatura Estricta**:
   - Interfaces en PascalCase con prefijo `I` (ej. `IMedicamentoService`, `IInsumoService`).
   - Métodos asíncronos en PascalCase terminados en `Async` (ej. `GetAllAsync`, `GetByIdAsync`, `CreateAsync`).
   - Términos de negocio en español coherente (`Medicamento`, `Dispensacion`, `Reposicion`), y tecnicismos de infraestructura en inglés (`DbContext`, `HttpClient`, `Middleware`).
5. **Tipos de Datos y Precisión**:
   - Para valores monetarios o costos, usá siempre `decimal` (nunca `float` ni `double`). En EF Core especificar `[Column(TypeName = "decimal(18,2)")]` o Fluent API.
   - Cantidades de stock deben ser enteros (`int`) con validación de no negatividad.
   - Fechas en UTC (`DateTime.UtcNow`).
6. **Modelos POCO Puros**:
   - Los modelos en `Models/` representan tablas y relaciones. No deben contener lógica HTTP ni reglas complejas.

---

## 5. Reglas RESTful y Pipeline HTTP

1. **Controladores Delgados**:
   - Decorados con `[ApiController]` y `[Route("api/[controller]")]`.
   - Heredan de `ControllerBase`.
   - Únicamente validan el binding de entrada, delegan al servicio y mapean el resultado a un `ActionResult<T>`.
   - **Cero lógica de negocio y cero consultas LINQ/EF en los controladores.**
2. **Semántica de Códigos HTTP**:
   - `GET`: `200 OK` con datos. Si no existe: `404 NotFound`.
   - `POST`: `201 Created` con `CreatedAtAction(nameof(GetById), new { id = ... }, resultado)`.
   - `PUT`: `204 NoContent` o `200 OK`. Si el recurso no existe: `404 NotFound`.
   - `DELETE`: `204 NoContent` al eliminar con éxito. Si no existe: `404 NotFound`.
   - Validaciones fallidas de entrada: `400 Bad Request`.
   - Conflictos de stock o estado: `409 Conflict`.
   - Falla de autenticación: `401 Unauthorized`.
   - Permiso insuficiente de rol: `403 Forbidden`.
   - Caída o fallo técnico de API hospitalaria externa: `502 Bad Gateway` o `503 Service Unavailable`.
3. **DTOs Obligatorios**:
   - Las entidades de `Models/` **NUNCA** se reciben directamente en un body de petición ni se devuelven crudas en respuestas que puedan tener navegación circular.
   - Definir `CreateDTO`, `UpdateDTO` y `ReadDTO` para cada recurso.
   - Los DTOs de entrada deben usar Data Annotations (`[Required]`, `[StringLength]`, `[Range]`).
4. **Filtrado Eficiente en Base de Datos**:
   - Los filtros (búsqueda por nombre, categoría, etc.) se procesan como `IQueryable<T>` en el servicio `...DbService`. **NUNCA** traigas colecciones completas a memoria RAM para filtrarlas con LINQ sobre `List<T>`.

---

## 6. Persistencia y Transaccionalidad (EF Core + SQLite)

1. **Persistencia Inicial**: SQLite configurado con `Microsoft.EntityFrameworkCore.Sqlite` y cadena de conexión en `appsettings.json`.
2. **I/O Asíncrono Completo**: Todas las operaciones con la base de datos deben ser asíncronas (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`, etc.).
3. **Consistencia Atómica de Stock y Dispensación**:
   - El descuento de stock y la creación de la dispensación deben ejecutarse dentro de una transacción (`await using var transaction = await context.Database.BeginTransactionAsync();`).
   - El stock nunca debe quedar negativo. Si la cantidad solicitada supera el stock disponible o la validación externa falla, la transacción debe cancelarse (Rollback) y devolver `409 Conflict`.
4. **Migraciones**: Todo cambio de esquema debe aplicarse mediante `dotnet ef migrations add <Nombre>` y `dotnet ef database update`.

---

## 7. Estrategia de Seguridad Híbrida

1. **Autenticación de Usuarios (JWT Bearer)**:
   - Implementar `IAuthService` y `AuthService` para registro y login.
   - Almacenar contraseñas hasheadas de forma segura usando `PasswordHasher<Usuario>` (PBKDF2).
   - Firmar tokens con HMAC-SHA256 utilizando `JWT_SECRET_KEY` cargada desde `.env` con `DotNetEnv`.
   - Proteger endpoints administrativos con `[Authorize(Roles = "Administrador")]`.
   - Habilitar autenticación Bearer en Swagger para testing interactivo.
2. **Integración Externa (API Key)**:
   - Implementar `ApiKeyMiddleware` para validar la cabecera `X-API-Key` en rutas dedicadas a sistemas externos o según lo exija la consigna.
   - La clave esperada se carga desde variables de entorno y se compara con tiempo constante.

---

## 8. Integración con la API Hospitalaria Externa

1. Encapsular toda interacción en `IHospitalApiClient` e implementarla en `ExternalServices/HospitalApiClient.cs`.
2. Utilizar `HttpClient` configurado mediante `IHttpClientFactory` (`builder.Services.AddHttpClient<IHospitalApiClient, HospitalApiClient>()`).
3. Definir DTOs internos para la respuesta de la receta.
4. Tratar fallos de conexión o timeouts con códigos `502`/`503`, diferenciándolos claramente de recetas rechazadas o inactivas (`400`/`422`).
5. **No registrar datos sensibles de salud ni recetas en los logs.**

---

## 9. Observabilidad, Logging y Manejo de Errores

1. **Logging Estructurado**: Usar siempre `ILogger<T>` con parámetros tipados:
   ```csharp
   logger.LogInformation("Dispensación {Id} registrada para el medicamento {MedicamentoId}", dispensacion.Id, medicamento.Id);
   ```
2. **Prohibición de `Console.WriteLine`**: No usar salidas por consola directas en código de producción.
3. **Middleware Global**: Atrapa excepciones no controladas en el inicio del pipeline, registra el error y responde con formato estructurado (evitando filtrar *stack traces* en entornos no de desarrollo).

---

## 10. Flujo de Trabajo y Criterios de Aceptación para el Agente

Antes de dar una tarea por terminada:
1. Comprobar compilación limpia con `dotnet build`.
2. Ejecutar y verificar que los tests pasen con `dotnet test`.
3. Comprobar que no existan advertencias graves de compilación ni referencias nulas no controladas.
4. Si se toma una nueva decisión técnica o se resuelven ambigüedades de la consigna, registrar un ADR en `docs/decisions/`.
