# Guía Maestra de Arquitectura Backend RESTful (C# .NET)
**De la Teoría al Código: Manual de Estructura, Tecnicismos, Buenas Prácticas y Plantilla para Futuros Proyectos**

---

## 1. Introducción y Filosofía Arquitectónica

Este documento sintetiza y formaliza la arquitectura backend desarrollada a lo largo del material de la cátedra de **Programación 2 (Servicios Backend - UAP)** y su materialización práctica en el proyecto `prog2-2026` (`BibliotecaApi`).

El propósito de esta guía es servir como **manual de ingeniería de software** para comprender en profundidad los conceptos, patrones de diseño y tecnicismos aplicados, así como proporcionar una **guía paso a paso y plantilla canónica** para replicar esta misma estructura en cualquier proyecto nuevo de backend profesional.

### 1.1 Principios Fundamentales de Diseño
1. **Agnóstico y Conceptual**: Aunque la implementación se realiza en C# con .NET, los conceptos (statelessness, REST, inyección de dependencias, pipeline de middlewares, capas desacopladas, persistencia relacional y seguridad basada en tokens) son universales y aplicables a cualquier stack moderno (Node.js/Express/NestJS, Python/FastAPI, Java/Spring Boot).
2. **Arquitectura MCS (Modelo - Controlador - Servicio)**: Evolución natural del patrón MVC adaptada a APIs RESTful donde no se generan vistas HTML sino respuestas serializadas (JSON).
3. **Desacoplamiento Estricto vía Contratos (Interfaces)**: Ninguna clase de alto nivel depende de implementaciones concretas; todas dependen de abstracciones. Esto permite sustituir el almacenamiento (de memoria a archivos JSON, y luego a SQLite/PostgreSQL) sin alterar una sola línea de los controladores.
4. **Semántica HTTP Rigurosa**: Uso preciso de verbos HTTP, códigos de estado, encabezados, orígenes de enlace (*model binding*) y URIs orientadas a recursos.
5. **Seguridad Stateless**: La API no almacena sesiones en memoria; cada petición transporta su credencial criptográfica autosuficiente (JWT).

```
                      ARQUITECTURA EN CAPAS (MCS)
                      
   [ Cliente HTTP / Frontend / Swagger ]
                  │  ▲
      HTTP Request│  │HTTP Response (JSON)
                  ▼  │
┌─────────────────┴──┴─────────────────────────────────────────────┐
│ 1. PIPELINE DE MIDDLEWARES (Program.cs)                          │
│    - Exception Handler (Atrapa errores no controlados)           │
│    - UseHttpsRedirection (Seguridad de transporte)               │
│    - UseAuthentication (¿Quién sos? Valida JWT)                  │
│    - UseAuthorization  (¿Qué podés hacer? Valida Roles/Políticas)│
│    - Endpoint Routing  (Enruta a la acción correspondiente)      │
└─────────────────┬──▲─────────────────────────────────────────────┘
                  │  │
                  ▼  │
┌─────────────────┴──┴─────────────────────────────────────────────┐
│ 2. CAPA DE PRESENTACIÓN / CONTROLADORES (Controllers/)           │
│    - Habla HTTP (Verbos, Status Codes, Headers)                  │
│    - Recibe DTOs y valida formato básico                         │
│    - NO contiene lógica de negocio ni llamadas directas a SQL    │
│    - Orquesta y delega al Servicio                               │
└─────────────────┬──▲─────────────────────────────────────────────┘
                  │  │ Recibe/Devuelve DTOs y Entidades
                  ▼  │
┌─────────────────┴──┴─────────────────────────────────────────────┐
│ 3. CAPA DE ABSTRACCIÓN / CONTRATOS (Interfaces/)                 │
│    - Interfaces puras (I...Service)                              │
│    - Definen el QUÉ, no el CÓMO                                  │
└─────────────────┬──▲─────────────────────────────────────────────┘
                  │  │ Implementa contrato
                  ▼  │
┌─────────────────┴──┴─────────────────────────────────────────────┐
│ 4. CAPA DE NEGOCIO Y PERSISTENCIA (Services/)                    │
│    - Implementación concreta (...DbService)                      │
│    - Lógica de dominio, reglas de negocio y transformaciones     │
│    - Ejecución asincrónica de operaciones (async / await)        │
│    - Interactúa con DbContext                                    │
└─────────────────┬──▲─────────────────────────────────────────────┘
                  │  │ Consultas y SaveChangesAsync()
                  ▼  │
┌─────────────────┴──┴─────────────────────────────────────────────┐
│ 5. CAPA DE DATOS Y DOMINIO (Context/ y Models/)                  │
│    - DbContext (Sesión de base de datos / Unit of Work)          │
│    - DbSet<T> (Representación de tablas)                         │
│    - Modelos de Dominio (POCOs con relaciones y claves)          │
└─────────────────┬──▲─────────────────────────────────────────────┘
                  │  │ SQL Queries (Generadas por EF Core)
                  ▼  │
┌─────────────────┴──┴─────────────────────────────────────────────┐
│ 6. BASE DE DATOS FÍSICA (SQLite / PostgreSQL / SQL Server)       │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. Glosario Exhaustivo de Tecnicismos y Conceptos Clave

A continuación se detallan los tecnicismos utilizados en el material pedagógico y en el código fuente, explicando su significado técnico, su propósito de diseño y su forma de uso.

### 2.1 Protocolo HTTP y Paradigma REST
*   **API (Application Programming Interface)**: Interfaz formal que define un contrato de comunicación entre programas. Permite que sistemas heterogéneos intercambien información sin conocer sus implementaciones internas.
*   **REST (Representational State Transfer)**: Estilo arquitectónico formulado por Roy Fielding (2000) basado en 6 restricciones: arquitectura cliente-servidor, sin estado (*stateless*), capacidad de caché, interfaz uniforme, sistema en capas y código bajo demanda (opcional).
*   **Stateless (Sin Estado)**: Principio según el cual el servidor no retiene memoria de las peticiones anteriores del cliente. Cada petición HTTP debe contener **toda** la información necesaria para ser entendida y procesada (parámetros, datos, tokens de autenticación). Esto permite la escalabilidad horizontal masiva (cualquier nodo del servidor puede atender cualquier petición).
*   **Recurso**: Entidad conceptual sobre la cual opera la API (ej. `Libro`, `Autor`, `Usuario`). En REST, los recursos se identifican mediante URIs y se manipulan mediante verbos HTTP estándar.
*   **URI / Endpoint**: Identificador uniforme del recurso. Sigue convenciones estrictas:
    *   Sustantivos en plural (`/api/libros`, `/api/autores`).
    *   Jerarquía para relaciones padre-hijo (`/api/autores/3/libros`).
    *   Query parameters para filtrado, búsqueda y paginación (`/api/libros?tema=novela&page=1`).
*   **Verbos HTTP**:
    *   `GET`: Recupera representaciones de recursos. Es **seguro** (no altera el estado del servidor) e **idempotente** (múltiples llamadas idénticas producen el mismo resultado).
    *   `POST`: Crea un nuevo recurso o ejecuta una acción. **No es seguro** y **no es idempotente** (ejecutarlo dos veces creará dos recursos).
    *   `PUT`: Reemplaza por completo el estado del recurso existente. Es **idempotente**.
    *   `PATCH`: Modifica parcialmente los atributos de un recurso existente.
    *   `DELETE`: Elimina el recurso especificado. Es **idempotente** (eliminar un recurso ya eliminado produce el mismo estado en la base de datos).
*   **Códigos de Estado HTTP (Status Codes)**:
    *   `200 OK`: Petición exitosa con cuerpo devuelto.
    *   `201 Created`: Recurso creado exitosamente (debe incluir cabecera `Location` con la URI del nuevo recurso).
    *   `204 No Content`: Petición exitosa sin cuerpo de retorno (ideal para `PUT` y `DELETE`).
    *   `400 Bad Request`: Datos de entrada inválidos, sintaxis JSON incorrecta o validación de modelo fallida.
    *   `401 Unauthorized`: Falta de autenticación (el cliente no presentó credenciales o el token es inválido/expiró).
    *   `403 Forbidden`: El cliente está autenticado pero su rol o permisos no le autorizan realizar la acción solicitada.
    *   `404 Not Found`: El recurso solicitado en la URI no existe.
    *   `409 Conflict`: Conflicto en el estado del recurso (ej. intento de registrar un nombre de usuario duplicado).
    *   `500 Internal Server Error`: Falla no controlada del lado del servidor.

### 2.2 Componentes de la Arquitectura MCS
*   **Modelo de Dominio (Entity / Domain Model)**: Clase C# plana (**POCO**: *Plain Old C# Object*) que representa la entidad del negocio y el esquema de la base de datos. Se nombra en singular (`Libro.cs`, `Autor.cs`). Solo declara propiedades y relaciones.
*   **DTO (Data Transfer Object)**: Objeto diseñado exclusivamente para transportar datos entre el cliente HTTP y el controlador. Resuelve problemas críticos:
    *   *Prevención de Over-Posting / Mass Assignment*: Evita que un cliente malintencionado envíe campos sensibles o de solo servidor (ej. `Id`, `Rol`, `EsAdmin`).
    *   *Ocultamiento de datos internos*: Evita enviar contraseñas en hash, marcas de tiempo internas o datos confidenciales.
    *   *Ruptura de ciclos de serialización*: Evita bucles infinitos en JSON causados por relaciones bidireccionales (`Autor -> Libros -> Autor`).
    *   *Especialización*: Se divide en DTOs de entrada (`LibroCreateDTO`, `LibroUpdateDTO`, `RegistroDto`, `LoginDto`) y de salida (`LibroReadDTO`, `AutorReadDTO`).
*   **Interfaz de Servicio (`I...Service`)**: Contrato que define qué métodos ofrece el servicio, sus parámetros y tipos de retorno, sin comprometer ninguna tecnología de persistencia. Permite aplicar el principio de Inversión de Dependencias y facilita el *testing* unitario con *mocks*.
*   **Servicio de Negocio (`...DbService` / `...Service`)**: Clase que contiene la lógica de negocio, validaciones de dominio y orquestación con la base de datos. No tiene dependencias de ASP.NET Core HTTP (`HttpRequest`, `HttpResponse`), lo que permite que sea testeable de forma aislada.
*   **Controlador (`...Controller`)**: Clase derivada de `ControllerBase` decorada con `[ApiController]` y `[Route]`. Su función es orquestar la petición HTTP: mapear parámetros, validar entrada básica, invocar al servicio y devolver un `ActionResult<T>` con el código de estado correspondiente.

### 2.3 Inversión de Control (IoC) e Inyección de Dependencias (DI)
*   **Inversión de Control (IoC)**: Principio arquitectónico donde el flujo de ejecución y la creación de objetos es gestionado por un contenedor de framework (*IoC Container*), no por la clase consumidora.
*   **Inyección de Dependencias (DI)**: Patrón de diseño donde los colaboradores que un objeto necesita son provistos desde el exterior (generalmente a través de los parámetros del constructor: *Constructor Injection*).
*   **Ciclos de Vida de los Servicios (Service Lifetimes)**:
    *   `AddTransient`: Se instancia un objeto nuevo cada vez que es solicitado por cualquier componente.
    *   `AddScoped`: Se crea una única instancia por cada petición HTTP (*Scope*). Todos los componentes dentro del mismo request comparten esa misma instancia, la cual se destruye al cerrarse la conexión HTTP. **Es el ciclo requerido para `DbContext` y servicios de negocio**.
    *   `AddSingleton`: Se crea una única instancia para toda la vida del proceso de la aplicación. Apropiado para servicios sin estado, cachés en memoria o clientes de mensajería compartidos.

### 2.4 Pipeline de Middlewares y Manejo de Errores
*   **Middleware**: Componente de software ensamblado en el pipeline de la aplicación para procesar solicitudes y respuestas HTTP. Se ejecutan en orden secuencial en la ida, y en orden inverso en el retorno.
*   **Pipeline Típico**:
    1. Manejo global de excepciones (`UseExceptionHandler` o Middleware personalizado).
    2. Redirección HTTPS (`UseHttpsRedirection`).
    3. Enrutamiento (`UseRouting`).
    4. Autenticación (`UseAuthentication`).
    5. Autorización (`UseAuthorization`).
    6. Mapeo de Controladores (`MapControllers`).
*   **Estrategia de Errores en Tres Capas**:
    1. *Capa 1 (Entrada)*: Filtros de validación automáticos de `[ApiController]` y Data Annotations devuelven `400 Bad Request` antes de ejecutar el controlador.
    2. *Capa 2 (Negocio)*: El controlador y el servicio gestionan flujos esperados (ej. recurso inexistente -> `404 NotFound`, conflicto de clave -> `409 Conflict`).
    3. *Capa 3 (Infraestructura/Global)*: Middleware al inicio del pipeline que atrapa cualquier excepción no controlada (`500 Internal Server Error`), escribe en el log y devuelve una respuesta estructurada (tipo RFC 7807 *ProblemDetails*) sin filtrar volcados de pila (*stack traces*) al exterior.

### 2.5 Principios SOLID en APIs Backend
*   **S - Single Responsibility Principle (SRP)**: Cada módulo o clase tiene un único motivo para cambiar. El Controlador cambia si cambian las rutas o convenciones HTTP; el Servicio cambia si cambian las reglas de negocio; el `DbContext` cambia si cambia el esquema relacional.
*   **O - Open/Closed Principle (OCP)**: Código abierto a la extensión pero cerrado a la modificación. Al programar contra la interfaz `iLibroService`, se pudo crear `LibroFileService` y luego `LibroDbService` sin modificar el código de `LibrosController`.
*   **L - Liskov Substitution Principle (LSP)**: Una implementación de una interfaz debe ser perfectamente intercambiable por otra sin alterar la corrección del programa ni lanzar excepciones inesperadas ante los mismos contratos de entrada y salida.
*   **I - Interface Segregation Principle (ISP)**: Muchas interfaces pequeñas y especializadas (`IAutorService`, `iLibroService`, `IFileStorageService`) son mejores que una única interfaz general sobrecargada.
*   **D - Dependency Inversion Principle (DIP)**: Los módulos de alto nivel (controladores) no deben depender de módulos de bajo nivel (servicios de base de datos concretos); ambos deben depender de abstracciones (interfaces).

### 2.6 Persistencia de Datos con ORM (Entity Framework Core)
*   **ORM (Object-Relational Mapper)**: Herramienta que traduce bidireccionalmente entre el modelo orientado a objetos (clases C#) y el modelo relacional (tablas SQL).
*   **`DbContext`**: Sesión lógica contra la base de datos que encapsula el patrón *Unit of Work* y gestiona la conexión, el seguimiento de cambios (*change tracking*) y las transacciones.
*   **`DbSet<T>`**: Representación en memoria de una tabla de la base de datos, proveyendo métodos LINQ para consultar (`ToListAsync`, `FirstOrDefaultAsync`, `Where`) y operaciones de mutación (`Add`, `Remove`).
*   **Data Annotations vs Fluent API**:
    *   *Data Annotations*: Atributos C# aplicados directamente sobre las clases y propiedades (`[Key]`, `[Required]`, `[MaxLength(100)]`, `[ForeignKey]`). Excelente para legibilidad y validación rápida.
    *   *Fluent API*: Configuración imperativa en el método `OnModelCreating` de `DbContext`. Ofrece control total para relaciones complejas, claves compuestas e índices. Tiene prioridad sobre las Data Annotations.
*   **Migraciones**: Sistema de control de versiones del esquema de base de datos. Transforma los cambios en las clases C# en scripts SQL de evolución (`Up`) y reversión (`Down`) reproducibles.
*   **Operaciones Asincrónicas (`async` / `await` / `Task<T>`)**: Mecanismo de Entrada/Salida no bloqueante (*non-blocking I/O*). Mientras la base de datos ejecuta la consulta, el hilo de ejecución de ASP.NET Core se libera y regresa al *thread pool* para atender otras peticiones entrantes. Esto maximiza el *throughput* y la concurrencia del servidor.
*   **Carga Ansiosa (*Eager Loading*) con `.Include()`**: Mecanismo para incluir entidades relacionadas en una sola consulta SQL (`JOIN`), evitando el problema del rendimiento de las $N+1$ consultas.

### 2.7 Autenticación y Autorización (JWT & Security)
*   **Autenticación (AuthN - "¿Quién sos?")**: Verificación de la identidad del cliente (mediante usuario y contraseña). Si es válida, el servidor emite una credencial firmada (Token JWT).
*   **Autorización (AuthZ - "¿Qué podés hacer?")**: Evaluación de permisos y roles del cliente autenticado para determinar si tiene acceso a un recurso o acción concreta.
*   **JWT (JSON Web Token - RFC 7519)**: Estructura de credencial compacta y autónoma (*self-contained*) dividida en tres partes codificadas en Base64Url separadas por puntos:
    $$\text{JWT} = \text{Header} \,.\, \text{Payload} \,.\, \text{Signature}$$
    *   *Header*: Algoritmo criptográfico (`alg`: `HS256`) y tipo (`typ`: `JWT`).
    *   *Payload*: Conjunto de *Claims* (afirmaciones) sobre el sujeto (`sub`, `name`, `role`, expiración `exp`). **No está cifrado, solo codificado**, por lo que nunca debe contener contraseñas ni datos sensibles.
    *   *Signature*: Hash criptográfico calculado combinando el Header, el Payload y una clave secreta privada (`Key`). Garantiza la **integridad** (que no fue alterado) y la **autenticidad** (que fue emitido por el servidor).
*   **Bearer Token**: Esquema HTTP de autorización donde quien porta el token (*the bearer*) recibe acceso al recurso. Se transmite en la cabecera HTTP: `Authorization: Bearer <token>`.
*   **Hashing Criptográfico de Contraseñas**: Práctica obligatoria de transformar la contraseña en texto plano en un hash irreversible utilizando funciones con sal (*Salt*) y factor de trabajo (*Work Factor*), como PBKDF2 (`PasswordHasher<T>`), BCrypt o Argon2.

### 2.8 Observabilidad y Documentación
*   **Logging Estructurado (`ILogger<T>`)**: Sistema de registro de eventos con parámetros enriquecidos, separando el mensaje del contexto. Permite su indexación en herramientas de monitoreo (Seq, Datadog, ElasticSearch).
*   **Niveles de Log**:
    *   `Trace` / `Debug`: Detalles diagnósticos de desarrollo.
    *   `Information`: Eventos normales de flujo de negocio ("Servicio iniciado", "Libro ID 12 creado").
    *   `Warning`: Comportamientos anómalos que no interrumpen el servicio.
    *   `Error`: Fallas en operaciones que impidieron completar un request.
    *   `Critical`: Fallas catastróficas que comprometen la continuidad de la aplicación (caída total de BD, agotamiento de memoria).
*   **OpenAPI y Swagger**: Especificación estándar para describir contratos de APIs HTTP de manera agnóstica al lenguaje. Swashbuckle genera dinámicamente el documento `swagger.json` y proporciona la interfaz web interactiva `Swagger UI` para pruebas de endpoints y simulación de autenticación Bearer JWT.

---

## 3. Análisis Crítico y Auditoría del Proyecto `prog2-2026`

El proyecto analizado (`BibliotecaApi`) implementa de forma fidedigna los principios teóricos expuestos en las cuatro unidades curriculares. No obstante, al realizar una auditoría de código profesional, se observan tanto **grandes aciertos de diseño** como **deudas técnicas e inconsistencias** que es fundamental conocer y corregir al aplicar esta estructura en nuevos proyectos.

### 3.1 Aciertos Destacados
1.  **Separación Limpia de Capas**: Cada responsabilidad reside en su carpeta correspondiente (`Models`, `DTOs`, `Interfaces`, `Services`, `Controllers`, `Context`).
2.  **Transición Demostrada de Persistencia**: El historial de commits evidencia la migración desde memoria -> `AutorFileService` (archivos JSON) -> `AutorDbService` (EF Core SQLite), confirmando la utilidad real del principio Abierto/Cerrado (OCP) y la Inversión de Dependencias (DIP).
3.  **Configuración de Seguridad JWT en Swagger**: `Program.cs` incluye la definición de seguridad `AddSecurityDefinition("Bearer", ...)` y `AddSecurityRequirement`, permitiendo probar endpoints protegidos directamente desde Swagger UI pegando el token generado en `/api/auth/login`.
4.  **Uso de C# Moderno**: Empleo de *Primary Constructors* (constructores primarios) en servicios y controladores (`public class AutorDbService(BibliotecaDbContext context)`), reduciendo código repetitivo (*boilerplate*).
5.  **Hashing Seguro**: Uso de `PasswordHasher<Usuario>` de ASP.NET Identity para almacenar contraseñas hasheadas en SQLite.

### 3.2 Oportunidades de Mejora y Deudas Técnicas a Evitar

| Componente | Código Actual en `prog2-2026` | Problema Técnico Detectado | Solución Recomendada para Nuevos Proyectos |
| :--- | :--- | :--- | :--- |
| **Convenciones de Nomenclatura** | `iLibroService.cs` (con minúscula), método `getAll()`. | Viola las directrices de C# / .NET (PascalCase para interfaces `ILibroService` y métodos `GetAll()`). Genera asimetría con `IAutorService`. | Mantener siempre nombres con PascalCase estricto: `ILibroService` y `GetAll()`. |
| **Filtrado en Memoria vs Base de Datos** | En `LibrosController.cs`: `var libros = await _libroService.getAll();` y luego `libros.FindAll(...)` | **Fuga de rendimiento grave**. Trae toda la tabla a memoria RAM del servidor y filtra en C# en lugar de ejecutar un `WHERE` filtrado en la base de datos SQL. | Pasar los parámetros de filtro al servicio (`_libroService.ObtenerFiltradosAsync(idAutor, tema)`) para que EF Core genere `WHERE` nativo en SQLite/PostgreSQL. |
| **Acoplamiento en AuthController** | `AuthController` inyecta directamente `BibliotecaDbContext`. | Viola el patrón MCS. El controlador de autenticación habla directamente con la base de datos saltándose la capa de servicio. | Crear una interfaz `IAuthService` y servicio `AuthService` para encapsular la lógica de registro, hashing y generación de JWT. |
| **Retorno de Entidades de Dominio en Endpoints** | Los métodos `GetById` y `GetAll` devuelven `ActionResult<Libro>` o `ActionResult<Autor>`. | Si se cargan relaciones con `.Include()`, expone esquemas internos y genera excepciones de ciclo de serialización JSON (`JsonException: Possible object cycle detected`). | Utilizar siempre DTOs de salida (`LibroReadDTO`, `AutorReadDTO`) proyectados con `.Select(...)` o AutoMapper. |
| **Validaciones Manuales vs Data Annotations** | `if (string.IsNullOrWhiteSpace(nuevoLibro.Titulo)) return BadRequest(...)` | Lógica de validación repetitiva en cada acción del controlador que ensucia el método. | Aplicar `[Required]`, `[StringLength]` en los DTOs. El framework (`[ApiController]`) rechaza automáticamente las peticiones inválidas con `400 Bad Request`. |
| **Manejo de Secretos** | Se utiliza `DotNetEnv` pero la clave JWT `jwt["Key"]` puede fallar si no existe archivo `.env`. | Riesgo de `NullReferenceException` al arrancar sin variable de entorno configurada. | Validar la existencia de la clave o utilizar *User Secrets* en desarrollo y variables de entorno del sistema en producción. |

---

## 4. Guía Paso a Paso: Cómo Crear un Nuevo Proyecto con esta Arquitectura

Para aplicar esta arquitectura a un nuevo proyecto (por ejemplo, una API para una tienda, una clínica o un sistema académico), seguí esta secuencia estandarizada.

### Paso 1: Creación del Proyecto e Instalación de Dependencias
Ejecutá los siguientes comandos en tu terminal:

```bash
# 1. Crear el proyecto Web API (desactivando controladores iniciales automáticos si se prefiere)
dotnet new webapi -n MiProyectoApi
cd MiProyectoApi

# 2. Instalar paquetes de Entity Framework Core y SQLite (o el motor deseado)
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design

# 3. Instalar soporte para Autenticación JWT Bearer
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# 4. Instalar paquetes de Documentación OpenAPI / Swagger
dotnet add package Swashbuckle.AspNetCore
dotnet add package DotNetEnv

# 5. Instalar la herramienta global de migraciones de EF Core (si no la tenés instalada)
dotnet tool install --global dotnet-ef
```

### Paso 2: Crear la Estructura de Directorios Canónica
Creá las siguientes carpetas en la raíz del proyecto:

```bash
mkdir Models DTOs Interfaces Services Context
```

El árbol resultante debe ser:
```
MiProyectoApi/
├── Context/             # Clases derivadas de DbContext (EF Core)
├── Controllers/         # Controladores HTTP (Endpoints)
├── DTOs/                # Data Transfer Objects (Create, Update, Read)
├── Interfaces/          # Contratos de servicios (I...Service)
├── Migrations/          # Migraciones generadas por EF Core
├── Models/              # Modelos de Dominio (POCOs / Tablas)
├── Services/            # Implementaciones de lógica de negocio (...DbService)
├── appsettings.json     # Configuración general y ConnectionStrings
├── .env                 # Variables de entorno y secretos locales
├── Program.cs           # Configuración del contenedor DI y Pipeline
└── MiProyectoApi.csproj
```

### Paso 3: Configuración (`appsettings.json` y `.env`)

**`appsettings.json`**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=miproyecto.db"
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Issuer": "MiProyectoApi",
    "Audience": "MiProyectoApiClients",
    "ExpiraMinutos": 60
  }
}
```

**`.env`** (agregar a `.gitignore`):
```env
JWT_SECRET_KEY=Esta_Es_Una_Clave_Secreta_Muy_Larga_Y_Segura_De_Al_Menos_32_Caracteres!
```

---

## 5. Plantilla de Código Maestra (Boilerplate Reutilizable)

A continuación se presenta el código limpio, estandarizado y libre de deudas técnicas para cada capa.

### 5.1 Capa de Modelos (`Models/`)

```csharp
// Models/Producto.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiProyectoApi.Models;

public class Producto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Precio { get; set; }

    public int CategoriaId { get; set; } // Clave foránea

    [ForeignKey(nameof(CategoriaId))]
    public Categoria? Categoria { get; set; } // Propiedad de navegación
}
```

```csharp
// Models/Categoria.cs
using System.ComponentModel.DataAnnotations;

namespace MiProyectoApi.Models;

public class Categoria
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public List<Producto> Productos { get; set; } = new();
}
```

```csharp
// Models/Usuario.cs
using System.ComponentModel.DataAnnotations;

namespace MiProyectoApi.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = "Usuario"; // "Usuario" o "Administrador"
}
```

---

### 5.2 Capa de Persistencia (`Context/`)

```csharp
// Context/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using MiProyectoApi.Models;

namespace MiProyectoApi.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Opcional: configuraciones avanzadas mediante Fluent API
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();
    }
}
```

---

### 5.3 Capa de DTOs (`DTOs/`)

```csharp
// DTOs/ProductoDTOs.cs
using System.ComponentModel.DataAnnotations;

namespace MiProyectoApi.DTOs;

public class ProductoCreateDTO
{
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Range(0.01, 1000000.00, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Precio { get; set; }

    [Required(ErrorMessage = "El ID de categoría es obligatorio.")]
    public int CategoriaId { get; set; }
}

public class ProductoUpdateDTO
{
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(150, MinimumLength = 3)]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Range(0.01, 1000000.00)]
    public decimal Precio { get; set; }

    public int CategoriaId { get; set; }
}

public class ProductoReadDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int CategoriaId { get; set; }
    public string NombreCategoria { get; set; } = string.Empty;
}
```

```csharp
// DTOs/AuthDTOs.cs
using System.ComponentModel.DataAnnotations;

namespace MiProyectoApi.DTOs;

public record RegistroDTO(
    [Required, StringLength(50, MinimumLength = 3)] string Usuario,
    [Required, StringLength(100, MinimumLength = 6)] string Password
);

public record LoginDTO(
    [Required] string Usuario,
    [Required] string Password
);

public record AuthResponseDTO(string Token, string Usuario, string Rol);
```

---

### 5.4 Capa de Contratos (`Interfaces/`)

```csharp
// Interfaces/IProductoService.cs
using MiProyectoApi.DTOs;

namespace MiProyectoApi.Interfaces;

public interface IProductoService
{
    Task<IEnumerable<ProductoReadDTO>> GetAllAsync(int? categoriaId = null, string? search = null);
    Task<ProductoReadDTO?> GetByIdAsync(int id);
    Task<ProductoReadDTO> CreateAsync(ProductoCreateDTO dto);
    Task<bool> UpdateAsync(int id, ProductoUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}
```

```csharp
// Interfaces/IAuthService.cs
using MiProyectoApi.DTOs;

namespace MiProyectoApi.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO?> LoginAsync(LoginDTO dto);
    Task<bool> RegisterAsync(RegistroDTO dto);
}
```

---

### 5.5 Capa de Servicios (`Services/`)

```csharp
// Services/ProductoDbService.cs
using Microsoft.EntityFrameworkCore;
using MiProyectoApi.Context;
using MiProyectoApi.DTOs;
using MiProyectoApi.Interfaces;
using MiProyectoApi.Models;

namespace MiProyectoApi.Services;

public class ProductoDbService(AppDbContext context, ILogger<ProductoDbService> logger) : IProductoService
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<ProductoDbService> _logger = logger;

    public async Task<IEnumerable<ProductoReadDTO>> GetAllAsync(int? categoriaId = null, string? search = null)
    {
        IQueryable<Producto> query = _context.Productos.Include(p => p.Categoria);

        if (categoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Nombre.ToLower().Contains(search.ToLower()));
        }

        return await query.Select(p => new ProductoReadDTO
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            CategoriaId = p.CategoriaId,
            NombreCategoria = p.Categoria != null ? p.Categoria.Nombre : string.Empty
        }).ToListAsync();
    }

    public async Task<ProductoReadDTO?> GetByIdAsync(int id)
    {
        var p = await _context.Productos
            .Include(prod => prod.Categoria)
            .FirstOrDefaultAsync(prod => prod.Id == id);

        if (p == null) return null;

        return new ProductoReadDTO
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            CategoriaId = p.CategoriaId,
            NombreCategoria = p.Categoria?.Nombre ?? string.Empty
        };
    }

    public async Task<ProductoReadDTO> CreateAsync(ProductoCreateDTO dto)
    {
        var producto = new Producto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            CategoriaId = dto.CategoriaId
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Producto {Nombre} creado con ID {Id}", producto.Nombre, producto.Id);

        return await GetByIdAsync(producto.Id) 
               ?? throw new InvalidOperationException("Error al recuperar el producto creado.");
    }

    public async Task<bool> UpdateAsync(int id, ProductoUpdateDTO dto)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return false;

        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.CategoriaId = dto.CategoriaId;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Producto ID {Id} actualizado", id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return false;

        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Producto ID {Id} eliminado", id);
        return true;
    }
}
```

```csharp
// Services/AuthService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiProyectoApi.Context;
using MiProyectoApi.DTOs;
using MiProyectoApi.Interfaces;
using MiProyectoApi.Models;

namespace MiProyectoApi.Services;

public class AuthService(AppDbContext context, IConfiguration config, ILogger<AuthService> logger) : IAuthService
{
    private readonly AppDbContext _context = context;
    private readonly IConfiguration _config = config;
    private readonly ILogger<AuthService> _logger = logger;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public async Task<bool> RegisterAsync(RegistroDTO dto)
    {
        if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == dto.Usuario))
        {
            return false; // Usuario ya existente
        }

        var usuario = new Usuario
        {
            NombreUsuario = dto.Usuario,
            Rol = "Usuario"
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, dto.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AuthResponseDTO?> LoginAsync(LoginDTO dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == dto.Usuario);
        if (usuario == null) return null;

        var result = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        string token = GenerarToken(usuario);
        return new AuthResponseDTO(token, usuario.NombreUsuario, usuario.Rol);
    }

    private string GenerarToken(Usuario usuario)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtSettings["Key"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiraMinutos"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

### 5.6 Capa de Controladores (`Controllers/`)

```csharp
// Controllers/ProductosController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiProyectoApi.DTOs;
using MiProyectoApi.Interfaces;

namespace MiProyectoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController(IProductoService productoService) : ControllerBase
{
    private readonly IProductoService _productoService = productoService;

    // GET: api/productos?categoriaId=1&search=teclado
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductoReadDTO>>> GetAll(
        [FromQuery] int? categoriaId, 
        [FromQuery] string? search)
    {
        var productos = await _productoService.GetAllAsync(categoriaId, search);
        return Ok(productos);
    }

    // GET: api/productos/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoReadDTO>> GetById(int id)
    {
        var producto = await _productoService.GetByIdAsync(id);
        if (producto == null) return NotFound(new { mensaje = $"Producto con ID {id} no encontrado." });
        return Ok(producto);
    }

    // POST: api/productos
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ProductoReadDTO>> Create([FromBody] ProductoCreateDTO nuevoProducto)
    {
        var productoCreado = await _productoService.CreateAsync(nuevoProducto);
        return CreatedAtAction(nameof(GetById), new { id = productoCreado.Id }, productoCreado);
    }

    // PUT: api/productos/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDTO productoActualizado)
    {
        var actualizado = await _productoService.UpdateAsync(id, productoActualizado);
        if (!actualizado) return NotFound(new { mensaje = $"Producto con ID {id} no encontrado." });
        return NoContent();
    }

    // DELETE: api/productos/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _productoService.DeleteAsync(id);
        if (!eliminado) return NotFound(new { mensaje = $"Producto con ID {id} no encontrado." });
        return NoContent();
    }
}
```

```csharp
// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using MiProyectoApi.DTOs;
using MiProyectoApi.Interfaces;

namespace MiProyectoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistroDTO dto)
    {
        bool registrado = await _authService.RegisterAsync(dto);
        if (!registrado) return Conflict(new { mensaje = "El nombre de usuario ya se encuentra registrado." });
        return Ok(new { mensaje = "Usuario registrado exitosamente." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginDTO dto)
    {
        var respuesta = await _authService.LoginAsync(dto);
        if (respuesta == null) return Unauthorized(new { mensaje = "Credenciales inválidas." });
        return Ok(respuesta);
    }
}
```

---

### 5.7 Archivo de Arranque Canónico (`Program.cs`)

```csharp
// Program.cs
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiProyectoApi.Context;
using MiProyectoApi.Interfaces;
using MiProyectoApi.Services;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVICIOS Y PERSISTENCIA (IoC Container)
// ==========================================

// Base de datos (EF Core)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Inyección de dependencias (Scoped por petición HTTP)
builder.Services.AddScoped<IProductoService, ProductoDbService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Controladores con serialización JSON
builder.Services.AddControllers();

// ==========================================
// 2. AUTENTICACIÓN Y AUTORIZACIÓN (JWT)
// ==========================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtSection["Key"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("La clave secreta de JWT (JWT_SECRET_KEY) no está configurada.");
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
        ClockSkew = TimeSpan.Zero // Expiración exacta sin tolerancia
    };
});

builder.Services.AddAuthorization();

// ==========================================
// 3. DOCUMENTACIÓN CON OPENAPI / SWAGGER
// ==========================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MiProyecto API",
        Version = "v1",
        Description = "API RESTful profesional construida con arquitectura MCS y ASP.NET Core"
    });

    // Configuración para autenticarse con Bearer Token en Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresá tu token JWT en el formato: Bearer {tu_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ==========================================
// 4. PIPELINE DE MIDDLEWARES (Orden crítico)
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MiProyecto API v1");
    });
}

app.UseHttpsRedirection();

// El orden de estos dos middlewares es INMUTABLE:
app.UseAuthentication(); // 1. ¿Quién sos?
app.UseAuthorization();  // 2. ¿Qué podés hacer?

app.MapControllers();

app.Run();
```

---

## 6. Flujo de Trabajo y Ciclo de Vida de las Migraciones

Una vez definidos los modelos y el `DbContext`, se debe versionar la base de datos:

```bash
# 1. Crear una nueva migración descriptiva
dotnet ef migrations add CreacionTablasIniciales

# 2. Aplicar los cambios a la base de datos (crea o actualiza el archivo .db)
dotnet ef database update

# 3. Si cometiste un error ANTES de hacer update:
dotnet ef migrations remove

# 4. Ver qué migraciones están aplicadas y cuáles pendientes:
dotnet ef migrations list
```

---

## 7. Checklist de Calidad para Cada Nuevo Requisito o Endpoint

Cada vez que agregues una nueva entidad o funcionalidad en futuros proyectos, verificá cumplir este checklist:

- [ ] **Modelo**: ¿La clase está en singular y define solo propiedades (POCO)? ¿Tiene configuradas claves foráneas y propiedades de navegación?
- [ ] **DbContext**: ¿Está declarado el `DbSet<T>` correspondiente en el contexto?
- [ ] **DTOs**: ¿Creaste DTOs de entrada (`CreateDTO`, `UpdateDTO`) y salida (`ReadDTO`)? ¿Tienen Data Annotations (`[Required]`, `[StringLength]`, `[Range]`)?
- [ ] **Interfaz**: ¿El contrato declara métodos asíncronos (`Task<T>`) y recibe/devuelve DTOs o tipos de dominio necesarios?
- [ ] **Servicio**: ¿Implementa la interfaz? ¿Usa `async`/`await` en todas las consultas y escrituras a BD? ¿Usa logging estructurado?
- [ ] **Inyección de Dependencias**: ¿Registraste el servicio en `Program.cs` como `AddScoped`?
- [ ] **Controlador**: ¿Hereda de `ControllerBase` y tiene `[ApiController]` y `[Route("api/[controller]")]`? ¿El nombre de la clase está en plural?
- [ ] **Códigos de Estado**: ¿Devuelve `200` para GET/PUT, `201` (`CreatedAtAction`) para POST, `204` para DELETE, `404` para no encontrado y `400` para datos inválidos?
- [ ] **Seguridad**: ¿Están protegidos con `[Authorize]` o `[Authorize(Roles = "...")]` los endpoints que modifican datos o exigen permisos?
- [ ] **Base de Datos**: ¿Ejecutaste `dotnet ef migrations add` y `dotnet ef database update`?
