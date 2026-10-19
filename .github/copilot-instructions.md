# Copilot / AI Assistant Instructions --- PharmaLink v2

Sos un asistente de programación experto en C# y ASP.NET Core (.NET 8/9). Al generar o sugerir código para PharmaLink, respetá las siguientes directrices sin excepción:

1. **Arquitectura Canónica MCS**:
   - `Controllers/` solo manejan HTTP y delegan a interfaces `I...Service`. No contienen reglas de negocio ni consultas directas a la base de datos.
   - `Services/*DbService` implementan la lógica de negocio y persistencia interactuando con `PharmaLinkContext`.
   - **NO generes una capa `Repositories/`** ni interfaces de repositorio. `DbContext` es la unidad de trabajo.
2. **C# Moderno y Estilo**:
   - Usá *Primary Constructors* para inyección de dependencias (`public class MedicamentoDbService(PharmaLinkContext context, ...) : IMedicamentoService`).
   - PascalCase estricto en métodos (`GetAllAsync`, `GetByIdAsync`) e interfaces (`IMedicamentoService`).
   - Usá `async` / `await` en todas las operaciones I/O.
   - Usá `decimal` para precios y monedas con `[Column(TypeName = "decimal(18,2)")]`.
3. **DTOs y Filtrado**:
   - Nunca recibas entidades de dominio en controladores ni las devuelvas directamente. Usá DTOs (`*CreateDTO`, `*UpdateDTO`, `*ReadDTO`).
   - Usá validaciones declarativas con Data Annotations (`[Required]`, `[StringLength]`, `[Range]`).
   - Filtrá siempre en la base de datos con `IQueryable.Where()`, nunca traigas listas completas a memoria con `FindAll`.
4. **Seguridad y Errores**:
   - Autenticación con JWT Bearer (`IAuthService`, contraseñas hasheadas con PBKDF2 `PasswordHasher<Usuario>`).
   - Protección de endpoints con `[Authorize]` y `[Authorize(Roles = "Administrador")]`.
   - Soporte para cabecera `X-API-Key` mediante middleware.
   - Códigos de estado HTTP semánticos: `200`, `201` (`CreatedAtAction`), `204`, `400`, `401`, `403`, `404`, `409`, `502`.
   - Logging estructurado con `ILogger<T>` (prohibido `Console.WriteLine`).
