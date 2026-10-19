# ADR-003: Persistencia con EF Core, SQLite y Control Transaccional

## Status
Accepted

## Date
2026-10-09

## Context
PharmaLink requiere persistir entidades relacionales (`Medicamento`, `Insumo`, `Dispensacion`, `Reposicion`, `Entrega`, `Usuario`), mantener integridad referencial y asegurar consistencia atómica en las operaciones de stock.

En el entorno académico y de evaluación de la cátedra de Programación 2, las bases de datos externas (como SQL Server, PostgreSQL o MySQL) introducen dependencias de infraestructura (servidores locales, contenedores Docker, credenciales de conexión fijas) que dificultan la reproducibilidad inmediata en las máquinas de los evaluadores.

Por otra parte, la operación de dispensación exige que el descuento de stock del medicamento y la creación del registro de dispensación ocurran de forma atómica: o se ejecutan ambos, o ninguno.

## Decision
1. **Motor de Persistencia**: Utilizar **SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`) con archivo local `pharmalink.db`.
2. **Versionado de Esquema**: Utilizar **Migraciones de EF Core** (`dotnet ef migrations add`, `dotnet ef database update`).
3. **Asincronía Total**: 100% de operaciones de base de datos implementadas con llamadas asíncronas (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`).
4. **Transacciones Explícitas para Stock**: Implementar `await using var transaction = await _context.Database.BeginTransactionAsync()` en `DispensacionDbService` para garantizar que la comprobación de stock, la deducción y la creación de la dispensación sean estrictamente atómicas. Si el stock es insuficiente o la validación externa falla, se ejecuta `RollbackAsync()`.

## Alternatives Considered

### 1. PostgreSQL o SQL Server
- **Pros**: Soporte completo para concurrencia empresarial y tipos decimal nativos de alta precisión.
- **Cons**: Requiere instalar un servidor o Docker; rompe la portabilidad del proyecto para los evaluadores.
- **Rejected**: SQLite es el estándar definido por la cátedra para la Unidad 4.

### 2. Almacenamiento en archivos JSON o en memoria
- **Pros**: Cero dependencias externas.
- **Cons**: No soporta transacciones ACID ni integridad referencial; no utiliza EF Core como exige el programa.
- **Rejected**: Incompatible con los requisitos de persistencia relacional.

## Consequences
- **Positivas**:
  - El proyecto es 100% autónomo y portable: cualquier clon del repositorio compila y crea la base de datos local al ejecutar `dotnet ef database update`.
  - Prevención garantizada de inconsistencias de stock gracias a transacciones ACID.
  - Fácil transición a PostgreSQL o SQL Server en el futuro cambiando únicamente el proveedor en `Program.cs`.
- **Negativas / Mitigaciones**:
  - SQLite tiene limitaciones nativas en el tipo `decimal`. Mitigación: Se configura la conversión a `double` en `OnModelCreating` de `PharmaLinkContext`.
