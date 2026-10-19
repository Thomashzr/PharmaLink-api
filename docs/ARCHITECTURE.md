# Arquitectura Canónica MCS --- PharmaLink

## 1. Visión General de la Arquitectura

PharmaLink implementa el patrón **MCS (Modelo - Controlador - Servicio)**, la arquitectura canónica de la cátedra de Programación 2 (UAP) para APIs RESTful profesionales en C# .NET.

A diferencia de patrones con sobreingeniería (como añadir una capa de repositorios sobre un ORM que ya es Unit of Work), esta arquitectura promueve:
- **Alta Cohesión y Bajo Acoplamiento**: Los controladores no conocen la base de datos; los servicios no conocen el protocolo HTTP.
- **Inversión de Dependencias (DIP)**: Todas las dependencias se inyectan a través de interfaces (`I...Service`).
- **Simplicidad Mantenible**: Menos capas pasamanos, código directo y altamente testeable mediante sustitución de interfaces o base de datos en memoria.

```
                      FLUJO ARQUITECTÓNICO MCS
                      
    [ Cliente HTTP / Frontend / Swagger / Postman ]
                   │  ▲
       HTTP Request│  │HTTP Response (JSON)
                   ▼  │
┌──────────────────┴──┴─────────────────────────────────────────────┐
│ 1. PIPELINE DE MIDDLEWARES (Program.cs)                           │
│    - ExceptionHandler / ExceptionMiddleware (Atrapa errores 500) │
│    - UseHttpsRedirection (Seguridad TLS)                          │
│    - ApiKeyMiddleware (Opcional para cabecera X-API-Key)         │
│    - UseAuthentication (Valida firma y expiración de JWT)         │
│    - UseAuthorization  (Verifica Roles/Claims)                   │
│    - MapControllers    (Enrutamiento a acciones)                  │
└──────────────────┬──▲─────────────────────────────────────────────┘
                   │  │
                   ▼  │
┌──────────────────┴──┴─────────────────────────────────────────────┐
│ 2. CONTROLADORES (Controllers/)                                   │
│    - Hablan HTTP: Verbos, Códigos de Estado, Headers              │
│    - Reciben DTOs con Data Annotations y validan binding          │
│    - Delegan toda la lógica a I...Service                         │
│    - Devuelven ActionResult<T> proyectando DTOs de salida         │
└──────────────────┬──▲─────────────────────────────────────────────┘
                   │  │ DTOs de Entrada / Salida
                   ▼  │
┌──────────────────┴──┴─────────────────────────────────────────────┐
│ 3. CONTRATOS E INTERFACES (Interfaces/)                           │
│    - IMedicamentoService, IInsumoService, IAuthService...         │
│    - IHospitalApiClient (para el servicio externo)                │
└──────────────────┬──▲─────────────────────────────────────────────┘
                   │  │ Implementación
                   ▼  │
┌──────────────────┴──┴─────────────────────────────────────────────┐
│ 4. SERVICIOS DE NEGOCIO Y PERSISTENCIA (Services/)                │
│    - MedicamentoDbService, DispensacionDbService, AuthService...  │
│    - Lógica de dominio, reglas de validación y cálculos           │
│    - Orquestación de transacciones atómicas                       │
│    - IQueryable eficiente sobre PharmaLinkContext                 │
└──────────────────┬──▲─────────────────────────────────────────────┘
                   │  │ LINQ Queries / SaveChangesAsync()
                   ▼  │
┌──────────────────┴──┴─────────────────────────────────────────────┐
│ 5. CONTEXTO Y MODELOS DE DOMINIO (Context/ y Models/)             │
│    - PharmaLinkContext (DbContext / Unit of Work)                 │
│    - DbSet<T> (Tablas relacionales)                               │
│    - Modelos POCO: Medicamento, Insumo, Dispensacion, Usuario... │
└──────────────────┬──▲─────────────────────────────────────────────┘
                   │  │ SQL Queries (Generadas por EF Core)
                   ▼  │
┌──────────────────┴──┴─────────────────────────────────────────────┐
│ 6. BASE DE DATOS FÍSICA (SQLite: pharmalink.db)                   │
└───────────────────────────────────────────────────────────────────┘
```

---

## 2. Estructura de Directorios Canónica

```text
PharmaLink/
├── Context/
│   └── PharmaLinkContext.cs            # DbContext: DbSets y configuración Fluent API
├── Controllers/
│   ├── AuthController.cs               # Endpoints /api/auth (register, login)
│   ├── MedicamentosController.cs       # Endpoints /api/medicamentos
│   ├── InsumosController.cs            # Endpoints /api/insumos
│   ├── DispensacionesController.cs     # Endpoints /api/dispensaciones
│   └── ReposicionesController.cs       # Endpoints /api/reposiciones
├── DTOs/
│   ├── Auth/                           # RegistroDTO, LoginDTO, AuthResponseDTO
│   ├── Medicamentos/                   # MedicamentoCreateDTO, MedicamentoUpdateDTO, MedicamentoReadDTO
│   ├── Insumos/                        # InsumoCreateDTO, InsumoUpdateDTO, InsumoReadDTO
│   ├── Dispensaciones/                 # DispensacionCreateDTO, DispensacionReadDTO
│   └── Reposiciones/                   # ReposicionCreateDTO, ReposicionReadDTO, ResumenReposicionDTO
├── Interfaces/
│   ├── IAuthService.cs
│   ├── IMedicamentoService.cs
│   ├── IInsumoService.cs
│   ├── IDispensacionService.cs
│   ├── IReposicionService.cs
│   └── IHospitalApiClient.cs           # Contrato para la API externa hospitalaria
├── Models/
│   ├── Usuario.cs                      # Entidad para autenticación y roles
│   ├── Medicamento.cs                  # Entidad POCO
│   ├── Insumo.cs                       # Entidad POCO
│   ├── Dispensacion.cs                 # Entidad POCO con FK a Medicamento
│   ├── Reposicion.cs                   # Entidad POCO con colección de Entregas
│   └── Entrega.cs                      # Entidad detalle de reposición
├── Services/
│   ├── AuthService.cs                  # Hash PBKDF2 y emisión de JWT
│   ├── MedicamentoDbService.cs         # Negocio y persistencia directa vía DbContext
│   ├── InsumoDbService.cs
│   ├── DispensacionDbService.cs        # Transacciones atómicas de stock y dispensación
│   └── ReposicionDbService.cs          # Máquina de estados y balance de stock
├── ExternalServices/
│   └── HospitalApiClient.cs            # Implementación con HttpClient tipado
├── Middleware/
│   ├── ExceptionMiddleware.cs          # Captura global de excepciones no controladas (500)
│   └── ApiKeyMiddleware.cs             # Validación de cabecera X-API-Key
├── Migrations/                         # Migraciones generadas por EF Core
├── appsettings.json                    # Cadenas de conexión y parámetros no sensibles
├── .env                                # Secretos (JWT_SECRET_KEY, API_KEY_SECRET)
└── Program.cs                          # Configuración del host, DI y pipeline
```

---

## 3. Matriz de Responsabilidades por Capa

| Capa / Archivo | Qué DEBE hacer | Qué NUNCA debe hacer |
| :--- | :--- | :--- |
| **Controllers** | Enrutamiento HTTP, lectura de parámetros/DTOs, llamar a `I...Service`, devolver `ActionResult<T>` con códigos de estado semánticos (`200`, `201`, `204`, `404`, `409`). | **NO** contiene lógica de negocio, **NO** calcula validaciones de dominio, **NO** inyecta `DbContext` ni ejecuta consultas SQL/LINQ directas. |
| **Interfaces** | Declarar métodos asíncronos (`Task<T>`) con parámetros tipados en DTOs o primitivos. | **NO** acoplarse a tecnologías de persistencia (ej. no exponer `IQueryable` crudo al exterior). |
| **Services (`...DbService`)** | Implementar la lógica del negocio, coordinar transacciones atómicas de stock, consultar y escribir en `PharmaLinkContext`, ejecutar validaciones de negocio y logging estructurado. | **NO** manipular objetos de transporte HTTP (`HttpRequest`, `HttpResponse`, `HttpContext`). |
| **Context (`PharmaLinkContext`)** | Mapear entidades con la base de datos relacional, configurar restricciones e índices en `OnModelCreating`, gestionar migraciones y transacciones. | **NO** contener reglas de negocio específicas de endpoints. |
| **Models** | POCOs planos con propiedades, tipos precisos (`decimal`, `int`, `DateTime`), claves foráneas y propiedades de navegación. | **NO** depender de HTTP ni de bibliotecas de presentación. |
| **DTOs** | Definir el contrato de entrada/salida de la API. Incorporar Data Annotations para validaciones declarativas de formato. | **NO** contener propiedades de navegación circulares que rompan la serialización JSON. |
| **ExternalServices** | Consumir la API HTTP del hospital mediante `IHttpClientFactory`, mapear respuesta a DTO interno, aislar timeouts y excepciones de red. | **NO** modificar directamente el stock local ni la base de datos de PharmaLink. |
| **Middleware** | Atrapado global de excepciones no controladas y validación previa de credenciales transversales. | **NO** manejar reglas de negocio específicas de una entidad. |

---

## 4. Registro de Dependencias y Ciclos de Vida (IoC)

En `Program.cs`, las dependencias se registran siguiendo estos principios:

```csharp
// 1. Contexto de persistencia (Scoped por petición HTTP)
builder.Services.AddDbContext<PharmaLinkContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Servicios de negocio (Scoped por petición HTTP)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMedicamentoService, MedicamentoDbService>();
builder.Services.AddScoped<IInsumoService, InsumoDbService>();
builder.Services.AddScoped<IDispensacionService, DispensacionDbService>();
builder.Services.AddScoped<IReposicionService, ReposicionDbService>();

// 3. Cliente HTTP Externo (Tipado y gestionado por IHttpClientFactory)
builder.Services.AddHttpClient<IHospitalApiClient, HospitalApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["HospitalApi:BaseUrl"] ?? "https://hospital.api.local");
    client.Timeout = TimeSpan.FromSeconds(10);
});
```

---

## 5. Justificación de la Eliminación de la Capa de Repositorios

En la versión previa del kit se consideró el patrón *Repository*. Sin embargo, como se detalla en el [ADR-001](decisions/ADR-001-adopcion-arquitectura-mcs-canonica.md):
1. `DbContext` ya implementa el patrón *Unit of Work*, y cada `DbSet<T>` es un *Repository* genérico provisto por Microsoft.
2. Añadir una capa intermedia de repositorios sobre EF Core genera duplicación innecesaria de métodos CRUD, oculta la potencia de LINQ (`IQueryable`, `Include`, `Select`) y complica la gestión de transacciones atómicas distribuidas entre múltiples entidades (como el descuento de stock y la creación de la dispensación).
3. La arquitectura MCS canónica de la cátedra sitúa la persistencia en `...DbService`, logrando un equilibrio óptimo entre desacoplamiento, claridad y testeabilidad.
