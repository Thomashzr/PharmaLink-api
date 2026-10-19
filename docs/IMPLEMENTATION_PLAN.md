# Plan de Implementación por Etapas --- PharmaLink

Este plan desglosa la construcción de PharmaLink en incrementos atómicos y verificables. El agente de código debe seguir estas etapas de manera secuencial, ejecutando `dotnet build` y `dotnet test` tras cada una.

---

## 🧭 Resumen de Etapas

| Etapa | Alcance | Verificación / Criterio de Éxito |
| :---: | :--- | :--- |
| **0** | **Inspección de Consigna y Contexto** | Confirmación de supuestos y lectura de ADRs. |
| **1** | **Esqueleto del Proyecto y Dependencias** | `dotnet build` exitoso con estructura de carpetas MCS. |
| **2** | **Modelos de Dominio y Persistencia EF Core** | `dotnet ef migrations add MigracionInicial` y base SQLite creada. |
| **3** | **Autenticación JWT y Módulo de Usuarios** | Registro, Login y Swagger interactivo con Bearer token. |
| **4** | **Gestión de Medicamentos e Insumos (CRUD + DTOs)** | Endpoints REST funcionando con filtrado LINQ en base de datos. |
| **5** | **Integración Externa Hospitalaria (HttpClient)** | Cliente `HospitalApiClient` aislado con tests simulados. |
| **6** | **Flujo Transaccional de Dispensaciones** | Validación de receta, decremento atómico de stock y tests de concurrencia. |
| **7** | **Flujo de Reposiciones e Inventario** | Máquina de estados (Solicitud -> Confirmación) y resumen de stock. |
| **8** | **Pipeline de Middlewares, Errores y Logging** | `ExceptionMiddleware`, `ApiKeyMiddleware` y logs estructurados. |
| **9** | **Suite de Testing Integral y Cierre** | 100% de tests unitarios y de integración pasando. |

---

## 🛠️ Detalle Paso a Paso

### Etapa 0: Inspección y Alineación
- Inspeccionar `reference/PharmaLink.pdf` (si está presente) o la consigna provista.
- Leer todos los documentos de `docs/` y los ADRs en `docs/decisions/`.
- Verificar que el entorno cuente con el SDK de .NET (`dotnet --version`).

### Etapa 1: Creación del Proyecto y Dependencias
1. Crear el proyecto Web API si no existe:
   ```bash
   dotnet new webapi -n PharmaLink
   cd PharmaLink
   ```
2. Instalar paquetes esenciales:
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add package Microsoft.EntityFrameworkCore.Design
   dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
   dotnet add package Swashbuckle.AspNetCore
   dotnet add package DotNetEnv
   ```
3. Crear las carpetas de la arquitectura canónica MCS:
   ```bash
   mkdir -p Context Controllers DTOs Interfaces Models Services ExternalServices Middleware
   ```
4. Configurar `.gitignore` para excluir `*.db`, `*.db-shm`, `*.db-wal`, `.env`, `bin/` y `obj/`.
5. Validar con `dotnet build`.

### Etapa 2: Modelos y Persistencia
1. Crear POCOs en `Models/`: `Usuario.cs`, `Medicamento.cs`, `Insumo.cs`, `Dispensacion.cs`, `Reposicion.cs`, `Entrega.cs`.
2. Crear `Context/PharmaLinkContext.cs` configurando `DbSet<T>` y Fluent API.
3. Configurar la cadena de conexión en `appsettings.json` (`Data Source=pharmalink.db`).
4. Generar y aplicar la primera migración:
   ```bash
   dotnet ef migrations add MigracionInicial
   dotnet ef database update
   ```

### Etapa 3: Autenticación y Seguridad
1. Crear DTOs en `DTOs/Auth/`: `RegistroDTO`, `LoginDTO`, `AuthResponseDTO`.
2. Crear interfaz `Interfaces/IAuthService.cs` y servicio `Services/AuthService.cs`.
3. Implementar hash con `PasswordHasher<Usuario>` y generación de JWT con `JWT_SECRET_KEY` desde `.env`.
4. Implementar `Controllers/AuthController.cs` con endpoints `/register` y `/login`.
5. Configurar autenticación y requerimiento Bearer en Swagger dentro de `Program.cs`.
6. Probar en Swagger que el login devuelva un token válido.

### Etapa 4: Medicamentos e Insumos
1. Crear DTOs para Medicamentos e Insumos (`CreateDTO`, `UpdateDTO`, `ReadDTO`).
2. Definir contratos `IMedicamentoService` e `IInsumoService`.
3. Implementar `MedicamentoDbService` e `InsumoDbService` con consultas LINQ optimizadas (filtrado en base de datos).
4. Implementar `MedicamentosController` e `InsumosController` con validación de roles (`[Authorize(Roles = "Administrador")]`).
5. Validar compilación y probar endpoints CRUD con códigos `200`, `201`, `204`, `404`.

### Etapa 5: Integración con la API del Hospital
1. Definir contrato `Interfaces/IHospitalApiClient.cs`.
2. Implementar `ExternalServices/HospitalApiClient.cs` consumiendo la API externa.
3. Registrar en `Program.cs` usando `AddHttpClient<IHospitalApiClient, HospitalApiClient>()`.
4. Aislar timeouts y excepciones de conectividad distinguiéndolos de rechazos funcionales de receta.

### Etapa 6: Flujo Transaccional de Dispensaciones
1. Crear DTOs en `DTOs/Dispensaciones/`.
2. Definir interfaz `IDispensacionService` e implementar `DispensacionDbService`.
3. Diseñar la transacción atómica:
   - Validar receta con hospital.
   - Verificar y descontar stock de medicamento.
   - Insertar registro de dispensación.
   - `CommitAsync` o `RollbackAsync` ante cualquier fallo.
4. Implementar `Controllers/DispensacionesController.cs`.
5. Escribir tests unitarios que verifiquen el rechazo ante stock insuficiente y ante receta inválida.

### Etapa 7: Reposiciones y Resumen de Inventario
1. Crear DTOs en `DTOs/Reposiciones/`.
2. Definir interfaz `IReposicionService` e implementar `ReposicionDbService`.
3. Implementar lógica de estados: alta de solicitud (`Solicitada`) y posterior confirmación (`Confirmada`) que incrementa stock.
4. Implementar cálculo del resumen de reposición (`/api/reposiciones/resumen`).
5. Implementar `Controllers/ReposicionesController.cs`.

### Etapa 8: Middlewares, Errores y Logging
1. Crear `Middleware/ExceptionMiddleware.cs` para atrapar fallos 500 y devolver JSON seguro.
2. Crear `Middleware/ApiKeyMiddleware.cs` para validar la cabecera `X-API-Key` en integraciones externas.
3. Configurar el orden exacto del pipeline en `Program.cs`.
4. Reemplazar cualquier salida de consola por logging estructurado con `ILogger<T>`.

### Etapa 9: Testing Automatizado y Verificación Final
1. Crear proyecto de tests:
   ```bash
   dotnet new xunit -n PharmaLink.Tests
   dotnet add PharmaLink.Tests reference PharmaLink/PharmaLink.csproj
   dotnet add PharmaLink.Tests package Moq
   dotnet add PharmaLink.Tests package Microsoft.EntityFrameworkCore.InMemory
   ```
2. Implementar los casos de prueba obligatorios definidos en `docs/TEST_PLAN.md`.
3. Ejecutar y validar:
   ```bash
   dotnet test
   ```
4. Documentar cualquier desvío o ajuste final en `docs/decisions/`.
