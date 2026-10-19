# PharmaLink Agent Kit v2 (Arquitectura Canónica MCS)

Este kit de especificaciones contiene la arquitectura, contratos, modelos, decisiones de diseño (ADRs) y reglas operativas para guiar a agentes de código de inteligencia artificial (como **Antigravity / `agy`**, GitHub Copilot o Claude Code) en la construcción de **PharmaLink** en C# (.NET 8/9).

---

## 🎯 Objetivo y Evolución respecto a v1

La versión 1 (`PharmaLink-agent-kit`) proponía una arquitectura que combinaba MCS con el patrón *Repository*. 
Esta **versión 2** evoluciona y moderniza el kit alineándolo al 100% con la **[Guía Maestra de Arquitectura Backend RESTful](../GUIA_ARQUITECTURA_BACKEND_RESTFUL.md)** (cátedra Programación 2 / UAP):

1. **MCS Canónico Puro (Eliminación de la capa `Repositories/`)**: `DbContext` de EF Core ya actúa como *Unit of Work* y almacén de repositorios (`DbSet<T>`). Los servicios de negocio (`Services/*DbService`) interactúan directamente con `PharmaLinkContext` en `Context/`, eliminando capas pasamanos redundantes.
2. **Seguridad Híbrida Robusta**: Se adopta el estándar de la guía con autenticación JWT Bearer (`IAuthService`, `AuthService`, roles `Usuario`/`Administrador`, hash con PBKDF2) para clientes y usuarios, manteniendo validación `X-API-Key` para integraciones de sistemas externos.
3. **C# Moderno y Buenas Prácticas**:
   - Constructores primarios (*Primary Constructors*).
   - Nomenclatura rigurosa en PascalCase (`IMedicamentoService`, `GetAllAsync`).
   - DTOs diferenciados (`CreateDTO`, `UpdateDTO`, `ReadDTO`) con validación declarativa (*Data Annotations*).
   - Filtrado obligatorio a nivel de base de datos (`IQueryable` con `.Where()`), erradicando filtrados en memoria en controladores.
   - Proyecciones eficientes con `.Select()` evitando ciclos de referencia circular en JSON.
4. **Decisiones Formalizadas como ADRs**: Conforme a la skill `documentation-and-adrs`, cada decisión técnica clave cuenta con un *Architecture Decision Record* formal en `docs/decisions/`.

---

## 📁 Estructura del Kit

```text
PharmaLink-agent-kit-v2/
├── README.md                           # Visión general y guía de uso del kit
├── AGENTS.md                           # Instrucciones directas y obligatorias para el agente
├── .github/
│   └── copilot-instructions.md         # Reglas resumidas para asistentes IDE (Copilot/Cursor)
└── docs/
    ├── PROJECT_BRIEF.md                # Requisitos del negocio, alcance y consigna
    ├── ARCHITECTURE.md                 # Arquitectura de capas MCS, árbol de carpetas y DI
    ├── DOMAIN_MODEL.md                 # Entidades POCO, relaciones, tipos decimal y claves
    ├── API_CONTRACT.md                 # Endpoints REST, verbos, DTOs y códigos HTTP
    ├── SECURITY_AND_ERRORS.md          # JWT Bearer, API Key, pipeline, middleware y logs
    ├── DATA_AND_INTEGRATION.md         # EF Core, SQLite, transacciones y Hospital API
    ├── TEST_PLAN.md                    # Estrategia de testing unitario y de integración
    ├── IMPLEMENTATION_PLAN.md          # Plan paso a paso para la implementación por etapas
    └── decisions/                      # Architecture Decision Records (ADRs)
        ├── README.md                   # Índice y ciclo de vida de ADRs
        ├── ADR-001-adopcion-arquitectura-mcs-canonica.md
        ├── ADR-002-estrategia-seguridad-hibrida-jwt-apikey.md
        ├── ADR-003-persistencia-efcore-sqlite-migraciones.md
        ├── ADR-004-dtos-y-mapeo-proyeccion.md
        └── ADR-005-manejo-errores-tres-capas-y-problem-details.md
```

---

## 🚀 Cómo Usar Este Kit con Antigravity (`agy`)

### 1. Copiar los archivos a la raíz del proyecto
Copiá los contenidos de este kit a la raíz de tu solución o repositorio:
```bash
cp -r PharmaLink-agent-kit-v2/* mi-proyecto-pharmalink/
```

### 2. Proporcionar la consigna oficial
Colocá el documento original de la consigna (ej. `PharmaLink.pdf` o documento de cátedra) en la carpeta `reference/` o en la raíz.

### 3. Instrucción inicial para el agente
Ejecutá `agy` en el directorio de trabajo entregando la siguiente instrucción:

> *"Leé `AGENTS.md` y todos los documentos de `docs/` (incluyendo los ADRs en `docs/decisions/`). Revisa la consigna en `reference/`. Resumí los requisitos funcionales y el plan de implementación, y luego procedé a implementar etapa por etapa según `docs/IMPLEMENTATION_PLAN.md`, validando con `dotnet build` y `dotnet test` en cada incremento."*

---

## ⚖️ Prevalencia de Fuentes de Verdad

1. **Consigna oficial del proyecto (`PharmaLink.pdf`) y aclaraciones del usuario.**
2. **Guía Maestra de Arquitectura Backend RESTful de la cátedra.**
3. **Este kit de especificaciones (`AGENTS.md`, `docs/`, `docs/decisions/`).**
4. Si se detecta cualquier conflicto o ambigüedad, el agente debe formular una consulta o registrar un nuevo ADR en `docs/decisions/`.
