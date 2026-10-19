# ADR-005: Manejo de Errores en Tres Capas y Respuestas Estructuradas

## Status
Accepted

## Date
2026-10-09

## Context
Una API RESTful profesional no debe filtrar nunca volcados de pila (*stack traces*), rutas de archivos del servidor ni detalles de excepciones internas hacia el exterior en respuestas HTTP. Además, los errores esperados (datos de entrada inválidos, recurso no encontrado o regla de negocio violada) deben devolverse con códigos de estado HTTP semánticos y un esquema de respuesta predecible para el cliente.

En la consigna original de PharmaLink se menciona una convención de error consistente en `{ "mensaje": "..." }` o `{ "code": "...", "message": "...", "details": "..." }`. La Guía Maestra de Arquitectura establece una estrategia defensiva en tres capas.

## Decision
Adoptar la **Estrategia de Errores en Tres Capas**:
1. **Capa 1: Validación de Entrada Automática**:
   - `[ApiController]` y Data Annotations capturan errores de formato de datos (campos nulos requeridos, tipos inválidos, cadenas que exceden longitud) y devuelven automáticamente `400 Bad Request` antes de que el controlador comience a ejecutarse.
2. **Capa 2: Errores de Negocio Esperados**:
   - El controlador traduce flujos conocidos:
     - Recurso inexistente -> `404 Not Found` con cuerpo `{ "mensaje": "El recurso con ID X no fue encontrado." }`.
     - Stock insuficiente o transición inválida -> `409 Conflict` con cuerpo `{ "mensaje": "Stock insuficiente para..." }`.
     - Falla de autenticación -> `401 Unauthorized`.
     - Rol insuficiente -> `403 Forbidden`.
3. **Capa 3: Middleware Global de Excepciones (`ExceptionMiddleware`)**:
   - Ubicado al inicio del pipeline en `Program.cs`. Atrapa cualquier error no controlado (falla de base de datos, `NullReferenceException`, desconexión externa).
   - Escribe el error detallado con `ILogger.LogError`.
   - Devuelve `500 Internal Server Error` (o `502 Bad Gateway` ante fallas de la API externa) con un mensaje genérico seguro y un identificador de seguimiento (`traceId`), sin filtrar *stack traces*.

## Alternatives Considered

### 1. Bloques try-catch en cada método de cada controlador
- **Pros**: Control local en cada acción.
- **Cons**: Duplicación masiva de código (*boilerplate*), código sucio, alto riesgo de omitir el bloque en alguna acción nueva dejando expuesto un stack trace.
- **Rejected**: Viola el principio DRY y las buenas prácticas de ASP.NET Core.

### 2. Devolver 200 OK con bandera `success: false`
- **Pros**: Fácil de leer por clientes muy simples.
- **Cons**: **Antipatrón RESTful severo**. Desprecia el protocolo HTTP y confunde a navegadores, proxies, balanceadores y herramientas como Swagger o Axios.
- **Rejected**: Inadmisible en una evaluación de servicios RESTful.

## Consequences
- **Positivas**:
  - Resiliencia y seguridad de la información: el servidor nunca revela vulnerabilidades de implementación ni contraseñas/rutas en excepciones.
  - El cliente recibe mensajes claros y semánticamente coherentes.
  - Logging centralizado y trazable para diagnóstico en desarrollo y producción.
