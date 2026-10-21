# ADR-001: Adopción de Arquitectura MCS Canónica (Sin Capa Repository)

## Status
Accepted

## Date
2026-10-09

## Context
En iteraciones tempranas de diseño, se evaluó una arquitectura híbrida que superponía el patrón *Repository* (`Repositories/` e interfaces `IRepository`) sobre Entity Framework Core, dividiendo la persistencia entre `Repositories/` y `Data/`.

Sin embargo, al contrastar esta propuesta con la **Guía Maestra de Arquitectura Backend RESTful (Cátedra Programación 2 / UAP)**, se observa que el estándar pedagógico y profesional promovido es el patrón **MCS (Modelo - Controlador - Servicio)** puro:
- Los controladores (`Controllers/`) son delgados y hablan exclusivamente HTTP.
- Los contratos (`Interfaces/`) definen los servicios de negocio (`I...Service`).
- Los servicios de negocio y persistencia (`Services/...DbService`) inyectan directamente el `DbContext` (`Context/`).
- `DbContext` de EF Core ya es por definición una implementación de los patrones *Unit of Work* y *Repository* (`DbSet<T>`).

Superponer otra capa de repositorios sobre EF Core agrega abstracciones pasamanos innecesarias, duplica firmas CRUD, restringe la expresividad de LINQ (`IQueryable`, `.Include()`, `.Select()`) y complica la coordinación de transacciones atómicas distribuidas entre múltiples entidades (como el descuento de stock de medicamentos al registrar una dispensación).

## Decision
Adoptar la arquitectura **MCS Canónica Pura**:
1. Eliminar la carpeta `Repositories/` y las interfaces de repositorio del proyecto.
2. Centralizar la persistencia en `Context/PharmaLinkContext.cs`.
3. Inyectar `PharmaLinkContext` directamente en las clases de servicio de negocio (`Services/*DbService`).
4. Los controladores interactúan únicamente con las interfaces `I...Service`, manteniendo total desacoplamiento de la infraestructura de persistencia.

## Alternatives Considered

### 1. Mantener el Patrón Repository tradicional
- **Pros**: Aislamiento teórico total de EF Core frente a los servicios; facilita reemplazar el ORM en un hipotético cambio de tecnología.
- **Cons**: Introduce clases pasamanos vacías que solo hacen `_context.Set<T>().Add()`; dificulta transacciones compuestas complejas; no es el patrón enseñado ni exigido en la guía de la cátedra.
- **Rejected**: Agrega sobreingeniería sin beneficio real en un proyecto académico donde EF Core es el estándar fijo.

### 2. Clean Architecture con CQRS y MediatR
- **Pros**: Máxima separación de responsabilidades y segregación de comandos y consultas.
- **Cons**: Curva de complejidad desmedida, proliferación exponencial de archivos (handlers, commands, queries), difícil de defender en una evaluación de Programación 2 y explícitamente desaconsejada en la guía.
- **Rejected**: Viola el principio de simplicidad y las restricciones explícitas de la cátedra.

## Consequences
- **Positivas**:
  - Código más conciso, limpio y fácil de mantener.
  - Mayor fluidez para armar transacciones atómicas con `context.Database.BeginTransactionAsync()`.
  - Capacidad nativa para proyectar directamente a DTOs en consultas LINQ (`.Select()`) sin traer entidades completas a memoria.
  - Perfecta alineación con el material de la cátedra y la Guía Maestra de Arquitectura.
- **Negativas / Mitigaciones**:
  - Los servicios dependen del paquete de EF Core (`Microsoft.EntityFrameworkCore`). Mitigación: Para testing unitario, se utiliza `Microsoft.EntityFrameworkCore.InMemory` o SQLite en memoria, lo que permite testear los servicios sin levantar una base de datos externa.
