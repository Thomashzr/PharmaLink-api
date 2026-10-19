# ADR-004: Separación Estricta de DTOs y Filtrado en Base de Datos

## Status
Accepted

## Date
2026-10-09

## Context
En la auditoría del proyecto de referencia (`prog2-2026 / BibliotecaApi`) analizado en la Guía Maestra de Arquitectura, se identificaron dos deudas técnicas críticas que degradan la seguridad y el rendimiento:
1. **Exposición directa de entidades de dominio en controladores**: Devolver entidades de EF Core genera excepciones de ciclo de serialización JSON (`JsonException: Possible object cycle detected`) cuando se cargan relaciones con `.Include()`, y recibir entidades como cuerpo permite ataques de asignación masiva (*Over-posting*).
2. **Filtrado en memoria RAM en controladores**: En el código auditado se ejecutaba `var libros = await _service.getAll();` y luego se filtraba en C# con `libros.FindAll(...)`. Esto traslada tablas completas a la memoria del servidor en lugar de ejecutar una cláusula `WHERE` filtrada directamente en el motor de base de datos.

## Decision
1. **Separación de DTOs en Tres Variantes por Recurso**:
   - `*CreateDTO`: Atributos requeridos para dar de alta un recurso, con Data Annotations para validación de entrada (`[Required]`, `[StringLength]`, `[Range]`).
   - `*UpdateDTO`: Atributos necesarios para modificar un recurso.
   - `*ReadDTO`: Estructura plana y segura devuelta al cliente, proyectando nombres de entidades relacionadas y excluyendo datos sensibles.
2. **Proyección Directa con LINQ (`.Select()`)**:
   - En los servicios `...DbService`, las consultas proyectan los campos directamente en el DTO de lectura en la consulta SQL (`query.Select(m => new MedicamentoReadDTO { ... }).ToListAsync()`), evitando instanciar entidades innecesarias.
3. **Filtrado Obligatorio en Base de Datos (`IQueryable`)**:
   - Los parámetros de búsqueda y filtro se pasan a los métodos del servicio (`GetAllAsync(string? search)`), construyendo expresiones `query = query.Where(...)` antes de materializar con `ToListAsync()`. Nunca se filtra sobre listas en memoria en los controladores.

## Alternatives Considered

### 1. AutoMapper u otras bibliotecas de mapeo
- **Pros**: Automatiza el copiado de propiedades con nombres idénticos.
- **Cons**: Introduce dependencias externas innecesarias, errores en tiempo de ejecución si cambian firmas, y añade complejidad pedagógica injustificada.
- **Rejected**: La proyección explícita con C# moderno es más legible, segura y libre de dependencias.

### 2. Devolver entidades POCO directamente y suprimir ciclos con JsonIgnore
- **Pros**: Menos clases para escribir.
- **Cons**: Acopla el esquema de base de datos con el contrato público de la API, arriesgando cambios accidentales en la API ante cambios en la base de datos.
- **Rejected**: Viola las buenas prácticas de diseño de APIs RESTful.

## Consequences
- **Positivas**:
  - Protección absoluta contra *over-posting* y ciclos de serialización JSON.
  - Alto rendimiento y bajo consumo de memoria RAM del servidor gracias a consultas SQL con `WHERE` nativo.
  - Validación declarativa automática provista por `[ApiController]` antes de llegar a la lógica de negocio.
- **Negativas / Mitigaciones**:
  - Mayor cantidad de archivos y clases DTO. Mitigación: Se organizan de forma limpia dentro de subcarpetas en `DTOs/` (`DTOs/Medicamentos/`, etc.).
