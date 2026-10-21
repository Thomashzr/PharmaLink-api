# PharmaLink API — Backend RESTful (Arquitectura Canónica MCS)

API RESTful profesional desarrollada en C# con **ASP.NET Core (.NET 10)** para la gestión integral de farmacia hospitalaria: catálogo de medicamentos, insumos médicos, flujo transaccional de dispensaciones vinculadas a recetas externas, órdenes de reposición con máquina de estados y autenticación de usuarios basada en roles.

El proyecto implementa la arquitectura canónica **MCS (Modelo - Controlador - Servicio)** descrita en la **[Guía Maestra de Arquitectura Backend RESTful](./GUIA_ARQUITECTURA_BACKEND_RESTFUL.md)** (cátedra Programación 2 / UAP).

---

## 🎯 Características Principales

1. **MCS Canónico Puro**:
   - `DbContext` de EF Core actúa como *Unit of Work* y almacén de repositorios (`DbSet<T>`).
   - Los servicios de negocio (`Services/*DbService`) gestionan la lógica y persistencia de datos directamente en `Context/PharmaLinkContext`, sin capas redundantes.
   - Controladores delgados (`Controllers/`) desacoplados con contratos de interfaz (`Interfaces/`).
2. **Seguridad Híbrida**:
   - **JWT Bearer**: Autenticación para usuarios y administradores (`/api/auth/register`, `/api/auth/login`) con hash PBKDF2 (`PasswordHasher<Usuario>`) y control de acceso basado en roles (`[Authorize(Roles = "Administrador")]`).
   - **X-API-Key**: Middleware selectivo (`ApiKeyMiddleware`) para integraciones externas de máquina a máquina con comparación segura en tiempo constante.
3. **Persistencia y Transaccionalidad**:
   - SQLite con Entity Framework Core (`PharmaLink.db`), migraciones versionadas y conversiones de tipo `decimal`.
   - Transacciones atómicas (`BeginTransactionAsync`) para dispensaciones con decremento de stock y reposiciones con acreditación de inventario.
4. **Integración Externa Hospitalaria**:
   - Cliente HTTP tipado (`HospitalApiClient`) con fallback a modo simulado configurable para desarrollo ágil y testing.
5. **Calidad y Testing**:
   - Suite completa de pruebas unitarias automatizadas con **xUnit** y **Moq** sobre base de datos SQLite en memoria.

---

## 📁 Estructura del Repositorio

```text
PharmaLink-api/
├── PharmaLink.slnx                     # Solución .NET
├── PharmaLink/                         # Proyecto Principal Web API
│   ├── Context/                        # PharmaLinkContext y Seeder inicial
│   ├── Controllers/                    # Controladores HTTP delgados
│   ├── DTOs/                           # Data Transfer Objects (Auth, Meds, Insumos, etc.)
│   ├── Exceptions/                     # Excepciones de negocio (RecetaInvalida, StockInsuficiente)
│   ├── ExternalServices/              # Clientes de integración externa (HospitalApiClient)
│   ├── Interfaces/                     # Contratos de servicios (DIP)
│   ├── Middleware/                     # ExceptionMiddleware global y ApiKeyMiddleware
│   ├── Migrations/                     # Migraciones versionadas de EF Core
│   ├── Models/                         # Entidades POCO de dominio
│   ├── Services/                       # Lógica de negocio y persistencia (...DbService)
│   ├── appsettings.json                # Configuración del entorno
│   └── Program.cs                      # Pipeline y contenedor de dependencias
├── PharmaLink.Tests/                   # Proyecto de Testing Automatizado (xUnit + Moq)
│   ├── Helpers/                        # TestDbContextFactory (SQLite in-memory)
│   └── Unit/                           # Pruebas unitarias de servicios
└── docs/                               # Documentación Técnica y Arquitectónica
    ├── ARCHITECTURE.md                 # Arquitectura de capas MCS y árbol de carpetas
    ├── DOMAIN_MODEL.md                 # Entidades, relaciones y reglas de integridad
    ├── API_CONTRACT.md                 # Endpoints REST, verbos, DTOs y respuestas HTTP
    ├── SECURITY_AND_ERRORS.md          # JWT Bearer, API Key, pipeline y logs
    ├── DATA_AND_INTEGRATION.md         # EF Core, SQLite y cliente hospitalario
    ├── TEST_PLAN.md                    # Matriz de casos de prueba y criterios de éxito
    └── decisions/                      # Architecture Decision Records (ADRs)
```

---

## 🚀 Puesta en Marcha

### Requisitos Previos
- [.NET SDK 10 o superior](https://dotnet.microsoft.com/)

### 1. Clonar el repositorio
```bash
git clone https://github.com/Thomashzr/PharmaLink-api.git
cd PharmaLink-api
```

### 2. Configurar variables de entorno (Opcional)
Copia la plantilla de secretos si deseas personalizar claves locales:
```bash
cp PharmaLink/.env.example PharmaLink/.env
```

### 3. Ejecutar la API
```bash
dotnet run --project PharmaLink
```
La aplicación inicializará automáticamente la base de datos SQLite local (`pharmalink.db`) con datos semilla para desarrollo (`admin` y `operador`).

Accede a la documentación interactiva en Swagger:
👉 **`https://localhost:7001/swagger`** o **`http://localhost:5000/swagger`**

---

## 🧪 Ejecución de Pruebas

Para ejecutar la suite completa de pruebas unitarias:
```bash
dotnet test
```

---

## 🔐 Credenciales Iniciales de Prueba (Seed Data)

| Usuario | Contraseña | Rol | Alcance |
| :--- | :--- | :--- | :--- |
| `admin` | `Admin123!` | `Administrador` | Acceso total (mutaciones de catálogo, confirmación de reposiciones) |
| `operador` | `Operador123!` | `Usuario` | Consultas de catálogo, dispensaciones y consulta de stock |

---

## 📚 Documentación y Decisiones Técnicas (ADRs)

Para profundizar en los lineamientos técnicos, consulta la carpeta `docs/decisions/`:
- **ADR-001**: Adopción de la Arquitectura Canónica MCS (Eliminación de Repositorios Redundantes).
- **ADR-002**: Estrategia de Seguridad Híbrida (JWT Bearer + API Key).
- **ADR-003**: Persistencia con EF Core, SQLite y Migraciones Versionadas.
- **ADR-004**: Mapeo y Proyecciones con DTOs sin dependencias externas.
- **ADR-005**: Manejo de Errores en 3 Capas y Formato Problem Details.
