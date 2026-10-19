# Registro de Decisiones de Arquitectura (ADRs) --- PharmaLink

Este directorio almacena los **Architecture Decision Records (ADRs)** de PharmaLink, siguiendo la convención estándar establecida por la skill `documentation-and-adrs`.

Cada documento captura el **por qué** detrás de una decisión técnica significativa: el contexto, los requisitos y restricciones analizados, las alternativas evaluadas con sus pros y contras, y las consecuencias positivas y negativas derivadas de la elección.

---

## 📋 Ciclo de Vida de un ADR

```
PROPOSED ──► ACCEPTED ──► (SUPERSEDED by ADR-XXX │ DEPRECATED)
```

- **Inmutabilidad histórica**: Nunca se eliminan ADRs aceptados del pasado. Si una decisión cambia, se redacta un nuevo ADR que reemplaza (*supersedes*) al anterior.

---

## 🗂️ Índice de Decisiones

| ID | Título | Estado | Fecha | Resumen de la Decisión |
| :---: | :--- | :---: | :---: | :--- |
| [**ADR-001**](ADR-001-adopcion-arquitectura-mcs-canonica.md) | Adopción de Arquitectura MCS Canónica (Sin capa Repository) | **Accepted** | 2026-10-09 | Adoptar Modelo-Controlador-Servicio canónico, interactuando los servicios directamente con `PharmaLinkContext` para eliminar capas redundantes. |
| [**ADR-002**](ADR-002-estrategia-seguridad-hibrida-jwt-apikey.md) | Estrategia de Seguridad Híbrida (JWT Bearer + API Key) | **Accepted** | 2026-10-09 | Autenticación JWT Bearer con roles para usuarios y Swagger, combinada con middleware `X-API-Key` para integraciones entre sistemas. |
| [**ADR-003**](ADR-003-persistencia-efcore-sqlite-migraciones.md) | Persistencia con EF Core, SQLite y Control Transaccional | **Accepted** | 2026-10-09 | Uso de SQLite como motor inicial académico, I/O 100% asíncrono y transacciones atómicas para dispensación y control de stock. |
| [**ADR-004**](ADR-004-dtos-y-mapeo-proyeccion.md) | Separación Estricta de DTOs y Filtrado en Base de Datos | **Accepted** | 2026-10-09 | Desacoplar modelos de dominio de los contratos HTTP mediante DTOs declarativos, proyectando con `.Select()` y filtrando en SQL. |
| [**ADR-005**](ADR-005-manejo-errores-tres-capas-y-problem-details.md) | Manejo de Errores en Tres Capas y Respuestas Estructuradas | **Accepted** | 2026-10-09 | Validación de modelo en Capa 1, errores de negocio en Capa 2 y Middleware global en Capa 3 para prevenir fugas de stack traces. |
