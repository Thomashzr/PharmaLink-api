# 📋 Plan de Implementación Paso a Paso — PharmaLink (v2 MCS Canónico)

> **Estado General:** `40% completado` (4 de 10 etapas completadas)  
> **Fecha de inicio:** 19 de Octubre de 2026  
> **Arquitectura:** MCS Canónico Puro (Modelo - Controlador - Servicio) en ASP.NET Core (.NET 10 / .NET 8 LTS)  
> **Persistencia:** Entity Framework Core con SQLite (`PharmaLink.db`)  
> **Seguridad:** Híbrida (JWT Bearer para usuarios/roles + `X-API-Key` para integraciones externas)

---

## 🧭 Tabla de Progreso por Etapas

| Etapa | Nombre | Estado | Archivos / Componentes Clave |
| :---: | :--- | :---: | :--- |
| **0** | **Inspección y Alineación de Requisitos** | `[x] Completada` | ADRs, `README.md`, SDK de .NET, verificación de entorno |
| **1** | **Esqueleto del Proyecto y Dependencias** | `[x] Completada` | `PharmaLink.csproj`, carpetas MCS, NuGet packages, `.gitignore` |
| **2** | **Modelos POCO y Persistencia EF Core** | `[x] Completada` | `Models/`, `Context/PharmaLinkContext.cs`, migraciones SQLite |
| **3** | **Autenticación JWT y Módulo de Usuarios** | `[x] Completada` | `IAuthService`, `AuthService`, `AuthController`, Swagger Bearer |
| **4** | **CRUD Medicamentos e Insumos + DTOs** | `[ ] Pendiente` | `MedicamentoDbService`, `InsumoDbService`, Controladores y RBAC |
| **5** | **Integración Externa API Hospital** | `[ ] Pendiente` | `IHospitalApiClient`, `HospitalApiClient`, `AddHttpClient` |
| **6** | **Flujo Transaccional de Dispensaciones** | `[ ] Pendiente` | Transacción atómica en `DispensacionDbService`, validación receta |
| **7** | **Flujo de Reposiciones e Inventario** | `[ ] Pendiente` | Máquina de estados (Solicitada -> Confirmada), Resumen stock |
| **8** | **Pipeline de Middlewares, Errores y Logs** | `[ ] Pendiente` | `ExceptionMiddleware`, `ApiKeyMiddleware`, `ILogger<T>` |
| **9** | **Testing Automatizado y Cierre** | `[ ] Pendiente` | `PharmaLink.Tests/`, unit tests, integración, `dotnet test` |

---

## ⚙️ Metodología de Desarrollo

1. **Avanzar una etapa a la vez**: Cada etapa es un incremento atómico y verificable.
2. **Validar antes de pasar a la siguiente**: Ejecutar `dotnet build` y las pruebas correspondientes antes de marcar los casilleros.
3. **Respetar la arquitectura MCS Canónica**:
   - 🚫 **No** crear capa `Repositories/` (EF Core `DbContext` ya es Unit of Work / Repository).
   - 🚫 **No** inyectar `DbContext` en los controladores (solo inyectar `I...Service`).
   - 🚫 **No** filtrar datos en memoria RAM (`List<T>`); usar siempre `IQueryable<T>` con LINQ en el servicio.
   - 🚫 **No** exponer entidades del dominio directamente en peticiones o respuestas; usar DTOs dedicados.
4. **Verificar el progreso**: Ejecutar `./generar_plan.sh` o `python3 scripts/generar_plan.py` en cualquier momento para comprobar el avance real de archivos en el disco.

---

## 📝 Detalle Granular de Tareas por Etapa

### Etapa 0: Inspección y Alineación de Requisitos
- [x] **0.1** Verificar disponibilidad de .NET SDK (`dotnet --version >= 8.0`).
- [x] **0.2** Leer `AGENTS.md` y comprender las restricciones canónicas MCS.
- [x] **0.3** Revisar los ADRs en `docs/decisions/` (ADR-001 al ADR-005).
- [x] **0.4** Confirmar convención de nombres y directrices de codificación en C# (PascalCase, Primary Constructors, `async/await`).

---

### Etapa 1: Esqueleto del Proyecto y Dependencias
- [x] **1.1** Crear el proyecto Web API:
  ```bash
  dotnet new webapi -n PharmaLink --use-controllers
  ```
- [x] **1.2** Instalar paquetes NuGet requeridos:
  ```bash
  cd PharmaLink
  dotnet add package Microsoft.EntityFrameworkCore.Sqlite
  dotnet add package Microsoft.EntityFrameworkCore.Design
  dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
  dotnet add package Swashbuckle.AspNetCore
  dotnet add package DotNetEnv
  ```
- [x] **1.3** Crear la estructura de directorios MCS Canónica:
  ```bash
  mkdir -p Context Controllers DTOs/Auth DTOs/Medicamentos DTOs/Insumos DTOs/Dispensaciones DTOs/Reposiciones Interfaces Models Services ExternalServices Middleware
  ```
- [x] **1.4** Configurar archivo `.gitignore` para excluir base de datos (`*.db`, `*.db-shm`, `*.db-wal`), secretos (`.env`) y carpetas `bin/`, `obj/`.
- [x] **1.5** Configurar plantilla inicial de `.env` (`.env.example`) con variables:
  - `JWT_SECRET_KEY=clave_super_secreta_de_al_menos_32_caracteres_123456`
  - `API_KEY_SECRET=pharmalink_hospital_api_key_secure_value_2026`
- [x] **1.6** Verificar compilación inicial limpia con `dotnet build`.

---

### Etapa 2: Modelos POCO y Persistencia EF Core
- [x] **2.1** Crear clase POCO `PharmaLink/Models/Usuario.cs` (`Id`, `NombreUsuario`, `PasswordHash`, `Rol`).
- [x] **2.2** Crear clase POCO `PharmaLink/Models/Medicamento.cs` (`Id`, `Nombre`, `Presentacion`, `StockDisponible`, `PrecioUnitario`, `Dispensaciones`).
- [x] **2.3** Crear clase POCO `PharmaLink/Models/Insumo.cs` (`Id`, `Nombre`, `Presentacion`, `StockDisponible`, `PrecioUnitario`, `Entregas`).
- [x] **2.4** Crear clase POCO `PharmaLink/Models/Dispensacion.cs` (`Id`, `CodigoReceta`, `MedicamentoId`, `Medicamento`, `Cantidad`, `FechaDispensacion`).
- [x] **2.5** Crear enumeración `EstadoReposicion` y clase POCO `PharmaLink/Models/Reposicion.cs` (`Id`, `FechaSolicitud`, `FechaConfirmacion`, `Estado`, `Entregas`).
- [x] **2.6** Crear clase POCO `PharmaLink/Models/Entrega.cs` (`Id`, `ReposicionId`, `Reposicion`, `InsumoId`, `Insumo`, `Cantidad`, `FechaEntrega`).
- [x] **2.7** Implementar `PharmaLink/Context/PharmaLinkContext.cs` configurando `DbSet<T>` y Fluent API:
  - Índice único en `Usuario.NombreUsuario`.
  - Conversión a `double` para columnas `decimal` en SQLite.
  - Reglas de integridad referencial (`DeleteBehavior.Restrict` en `Dispensacion.Medicamento` y `Entrega.Insumo`, `DeleteBehavior.Cascade` en `Entrega.Reposicion`).
- [x] **2.8** Configurar cadena de conexión en `PharmaLink/appsettings.json` (`Data Source=pharmalink.db`).
- [x] **2.9** Registrar `PharmaLinkContext` en `PharmaLink/Program.cs` usando `builder.Services.AddDbContext<PharmaLinkContext>()`.
- [x] **2.10** Generar y aplicar la migración inicial:
  ```bash
  dotnet ef migrations add MigracionInicial
  dotnet ef database update
  ```
- [x] **2.11** Verificar compilación y creación del archivo `pharmalink.db`.

---

### Etapa 3: Autenticación JWT y Módulo de Usuarios
- [x] **3.1** Crear DTOs de autenticación en `PharmaLink/DTOs/Auth/`:
  - `RegistroDTO.cs` (`Usuario`, `Password`)
  - `LoginDTO.cs` (`Usuario`, `Password`)
  - `AuthResponseDTO.cs` (`Token`, `Usuario`, `Rol`)
- [x] **3.2** Definir interfaz `PharmaLink/Interfaces/IAuthService.cs`:
  - `Task<AuthResponseDTO?> RegisterAsync(RegistroDTO dto);`
  - `Task<AuthResponseDTO?> LoginAsync(LoginDTO dto);`
- [x] **3.3** Implementar servicio `PharmaLink/Services/AuthService.cs`:
  - Uso de Primary Constructor con `PharmaLinkContext`, `IConfiguration` e `ILogger<AuthService>`.
  - Hashing seguro con `PasswordHasher<Usuario>` (PBKDF2).
  - Generación de token JWT con HMAC-SHA256, expiración configurable y Claims (`NameIdentifier`, `Name`, `Role`).
- [x] **3.4** Configurar autenticación JWT Bearer en `PharmaLink/Program.cs`:
  - Cargar variables de entorno con `DotNetEnv.Env.Load()`.
  - Configurar `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`.
  - Configurar `TokenValidationParameters` con `IssuerSigningKey`.
  - Configurar Swashbuckle Swagger con `AddSecurityDefinition("Bearer", ...)` y `AddSecurityRequirement(...)`.
- [x] **3.5** Implementar `PharmaLink/Controllers/AuthController.cs`:
  - Endpoint `POST /api/auth/register` (devuelve `200 OK` con token o `400 Bad Request` si usuario ya existe).
  - Endpoint `POST /api/auth/login` (devuelve `200 OK` con token o `401 Unauthorized` si credenciales son inválidas).
- [x] **3.6** Validar registro y login mediante compilación y verificar token en Swagger.

---

### Etapa 4: CRUD de Medicamentos e Insumos
- [ ] **4.1** Crear DTOs para Medicamentos en `PharmaLink/DTOs/Medicamentos/`:
  - `MedicamentoCreateDTO.cs`, `MedicamentoUpdateDTO.cs`, `MedicamentoReadDTO.cs`.
- [ ] **4.2** Crear DTOs para Insumos en `PharmaLink/DTOs/Insumos/`:
  - `InsumoCreateDTO.cs`, `InsumoUpdateDTO.cs`, `InsumoReadDTO.cs`.
- [ ] **4.3** Definir interfaces en `PharmaLink/Interfaces/`:
  - `IMedicamentoService.cs` (`GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`).
  - `IInsumoService.cs` (`GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`).
- [ ] **4.4** Implementar `PharmaLink/Services/MedicamentoDbService.cs`:
  - Primary Constructor `(PharmaLinkContext context, ILogger<MedicamentoDbService> logger)`.
  - Búsqueda filtrada con `IQueryable<Medicamento>` y `.Where()` en BD (no en memoria RAM).
  - Proyecciones `.Select()` a `MedicamentoReadDTO`.
  - Validación antes de eliminar: impedir borrado si existen dispensaciones asociadas.
- [ ] **4.5** Implementar `PharmaLink/Services/InsumoDbService.cs`:
  - Primary Constructor `(PharmaLinkContext context, ILogger<InsumoDbService> logger)`.
  - Filtrado en BD con `IQueryable` y mapeo a DTOs.
  - Validación de existencia antes de actualizar o eliminar.
- [ ] **4.6** Registrar servicios en `PharmaLink/Program.cs` (`AddScoped<IMedicamentoService, MedicamentoDbService>`, etc.).
- [ ] **4.7** Implementar controladores delgados:
  - `PharmaLink/Controllers/MedicamentosController.cs` (anónimo para lectura, `[Authorize(Roles = "Administrador")]` para mutaciones).
  - `PharmaLink/Controllers/InsumosController.cs` (anónimo para lectura, `[Authorize(Roles = "Administrador")]` para mutaciones).
- [ ] **4.8** Verificar compilación con `dotnet build` y probar respuestas `200`, `201`, `204`, `400`, `404`.

---

### Etapa 5: Integración con la API del Hospital
- [ ] **5.1** Definir DTOs del servicio hospitalario externo en `PharmaLink/DTOs/External/`:
  - `RecetaValidacionResultDTO.cs` (`EsValida`, `CodigoReceta`, `MedicamentoSugerido`, `Mensaje`).
- [ ] **5.2** Definir interfaz `PharmaLink/Interfaces/IHospitalApiClient.cs`:
  - `Task<RecetaValidacionResultDTO> ValidarRecetaAsync(string codigoReceta);`
- [ ] **5.3** Implementar `PharmaLink/ExternalServices/HospitalApiClient.cs`:
  - Primary Constructor `(HttpClient httpClient, IConfiguration configuration, ILogger<HospitalApiClient> logger)`.
  - Lectura de URL base desde configuración (`HospitalApi:BaseUrl`).
  - Manejo de timeouts y fallos de conectividad (`HttpRequestException`) lanzando o retornando estado adecuado sin filtrar datos médicos en logs.
- [ ] **5.4** Registrar cliente tipado en `PharmaLink/Program.cs`:
  - `builder.Services.AddHttpClient<IHospitalApiClient, HospitalApiClient>(...);`
- [ ] **5.5** Verificar compilación limpia.

---

### Etapa 6: Flujo Transaccional de Dispensaciones
- [ ] **6.1** Crear DTOs en `PharmaLink/DTOs/Dispensaciones/`:
  - `DispensacionCreateDTO.cs` (`CodigoReceta`, `MedicamentoId`, `Cantidad`).
  - `DispensacionReadDTO.cs` (`Id`, `CodigoReceta`, `MedicamentoId`, `NombreMedicamento`, `Cantidad`, `FechaDispensacion`).
- [ ] **6.2** Definir interfaz `PharmaLink/Interfaces/IDispensacionService.cs`:
  - `Task<DispensacionReadDTO> RegistrarDispensacionAsync(DispensacionCreateDTO dto);`
  - `Task<DispensacionReadDTO?> GetByIdAsync(int id);`
  - `Task<IEnumerable<DispensacionReadDTO>> GetAllAsync();`
- [ ] **6.3** Implementar servicio transaccional `PharmaLink/Services/DispensacionDbService.cs`:
  - Inyectar `PharmaLinkContext`, `IHospitalApiClient`, `ILogger<DispensacionDbService>`.
  - 1º Validar receta con `IHospitalApiClient.ValidarRecetaAsync(dto.CodigoReceta)`. Si no es válida -> lanzar excepción de negocio o retornar resultado de conflicto.
  - 2º Abrir transacción de base de datos (`await using var transaction = await context.Database.BeginTransactionAsync()`).
  - 3º Consultar medicamento y verificar stock: `medicamento.StockDisponible >= dto.Cantidad`. Si es insuficiente -> abortar con `409 Conflict`.
  - 4º Descontar stock: `medicamento.StockDisponible -= dto.Cantidad`.
  - 5º Crear y agregar registro `Dispensacion`.
  - 6º `await context.SaveChangesAsync(); await transaction.CommitAsync();`.
- [ ] **6.4** Implementar `PharmaLink/Controllers/DispensacionesController.cs`:
  - Decorado con `[Authorize]`.
  - Endpoint `POST /api/dispensaciones` devolviendo `201 Created` o `409 Conflict`.
  - Endpoint `GET /api/dispensaciones/{id}` y `GET /api/dispensaciones`.
- [ ] **6.5** Registrar servicio en `Program.cs` y validar compilación.

---

### Etapa 7: Flujo de Reposiciones e Inventario
- [ ] **7.1** Crear DTOs en `PharmaLink/DTOs/Reposiciones/`:
  - `EntregaItemDTO.cs` (`InsumoId`, `Cantidad`).
  - `ReposicionCreateDTO.cs` (`Items`).
  - `ReposicionReadDTO.cs` (`Id`, `FechaSolicitud`, `FechaConfirmacion`, `Estado`, `Entregas`).
  - `EntregaDetalleDTO.cs` (`Id`, `InsumoId`, `NombreInsumo`, `Cantidad`, `FechaEntrega`).
  - `ResumenReposicionDTO.cs` (`TotalSolicitudes`, `TotalConfirmadas`, `TotalPendientes`, `StockInsumos`).
  - `InsumoStockDTO.cs` (`InsumoId`, `Nombre`, `StockActual`).
- [ ] **7.2** Definir interfaz `PharmaLink/Interfaces/IReposicionService.cs`:
  - `Task<ReposicionReadDTO> CrearSolicitudAsync(ReposicionCreateDTO dto);`
  - `Task<ReposicionReadDTO?> GetByIdAsync(int id);`
  - `Task<ReposicionReadDTO> ConfirmarReposicionAsync(int id);`
  - `Task<ResumenReposicionDTO> ObtenerResumenAsync();`
- [ ] **7.3** Implementar servicio `PharmaLink/Services/ReposicionDbService.cs`:
  - Máquina de estados: Solo pasar de `Solicitada` a `Confirmada`.
  - Al solicitar: Crear reposición e ítems de entrega en estado `Solicitada` (NO alterar stock aún).
  - Al confirmar: Dentro de transacción EF Core, verificar estado actual, marcar `Confirmada`, asignar `FechaConfirmacion = DateTime.UtcNow` e **incrementar el stock** de cada `Insumo`.
  - Impedir re-confirmación (evitar duplicación de stock, devolver `409 Conflict`).
  - Cálculo eficiente en base de datos para `ObtenerResumenAsync`.
- [ ] **7.4** Implementar `PharmaLink/Controllers/ReposicionesController.cs`:
  - `POST /api/reposiciones` (`[Authorize(Roles = "Administrador")]`) -> `201 Created`.
  - `GET /api/reposiciones/{id}` (`[Authorize]`) -> `200 OK`.
  - `POST /api/reposiciones/{id}/confirmar` (`[Authorize(Roles = "Administrador")]`) -> `200 OK`.
  - `GET /api/reposiciones/resumen` (`[Authorize]`) -> `200 OK`.
- [ ] **7.5** Registrar servicio en `Program.cs` y validar compilación.

---

### Etapa 8: Middlewares, Manejo Global de Errores y Logging
- [ ] **8.1** Crear `PharmaLink/Middleware/ExceptionMiddleware.cs`:
  - Atrapar excepciones no controladas en el pipeline.
  - Logging del error estructurado con `ILogger<ExceptionMiddleware>`.
  - Respuesta HTTP `500 Internal Server Error` o adaptada con `ProblemDetails` / JSON seguro (sin filtrar stack traces en producción).
- [ ] **8.2** Crear `PharmaLink/Middleware/ApiKeyMiddleware.cs`:
  - Validar cabecera `X-API-Key` contra `API_KEY_SECRET` para endpoints designados a integración externa.
  - Comparación en tiempo constante para mitigar ataques de tiempo (*timing attacks*).
- [ ] **8.3** Configurar orden del pipeline en `PharmaLink/Program.cs`:
  1. `UseMiddleware<ExceptionMiddleware>()`
  2. Swagger (si es Development)
  3. `UseHttpsRedirection()`
  4. `UseRouting()`
  5. `UseAuthentication()`
  6. `UseAuthorization()`
  7. `UseMiddleware<ApiKeyMiddleware>()` (o condicional por endpoint)
  8. `MapControllers()`
- [ ] **8.4** Auditar que no exista ningún `Console.WriteLine` en el código y todo use `ILogger<T>`.

---

### Etapa 9: Testing Automatizado y Verificación Final
- [ ] **9.1** Crear proyecto de pruebas unitarias e integración:
  ```bash
  dotnet new xunit -n PharmaLink.Tests
  dotnet add PharmaLink.Tests reference PharmaLink/PharmaLink.csproj
  dotnet add PharmaLink.Tests package Moq
  dotnet add PharmaLink.Tests package Microsoft.EntityFrameworkCore.InMemory
  dotnet add PharmaLink.Tests package FluentAssertions
  ```
- [ ] **9.2** Implementar pruebas de Autenticación:
  - Registro de usuario y validación de hash PBKDF2.
  - Login con contraseña correcta e incorrecta.
- [ ] **9.3** Implementar pruebas de Dispensación Transaccional:
  - Dispensación exitosa descuenta stock del medicamento.
  - Rechazo con stock insuficiente (rollback y excepción/conflicto).
  - Rechazo ante receta inválida por API hospitalaria.
- [ ] **9.4** Implementar pruebas de Reposición y Máquina de Estados:
  - Solicitud no modifica stock de insumos de inmediato.
  - Confirmación incrementa stock exactamente en la cantidad entregada.
  - Intento de reconfirmar reposición ya confirmada arroja conflicto.
- [ ] **9.5** Ejecutar suite completa de tests:
  ```bash
  dotnet test
  ```
- [ ] **9.6** Registrar cualquier decisión de ajuste final como ADR en `docs/decisions/`.

---

## 🚀 Cómo Empezar Ahora Mismo

Para comenzar el desarrollo, el primer paso es ejecutar la **Etapa 1**:
```bash
dotnet new webapi -n PharmaLink --use-controllers
```
A medida que vayamos completando tareas, iremos actualizando este archivo marcando `[x]` en cada casilla correspondiente y validando la compilación en cada incremento.
