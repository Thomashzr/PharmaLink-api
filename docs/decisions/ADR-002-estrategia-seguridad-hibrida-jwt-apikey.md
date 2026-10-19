# ADR-002: Estrategia de Seguridad Híbrida (JWT Bearer + API Key)

## Status
Accepted

## Date
2026-10-09

## Context
Existen dos requerimientos de seguridad con orígenes distintos:
1. La **consigna original de PharmaLink** menciona la protección de la API mediante la cabecera `X-API-Key` para autorizar llamadas entrantes.
2. La **Guía Maestra de Arquitectura Backend RESTful** estandariza la autenticación mediante **Tokens JWT Bearer**, con un modelo de usuarios (`Usuario`), contraseñas hasheadas (`PasswordHasher<T>`), endpoints `/api/auth/register` y `/api/auth/login`, control de roles (`Usuario` y `Administrador`), y configuración interactiva en Swagger UI.

Elegir solo uno de los dos mecanismos generaría un dilema:
- Si solo usamos API Key, perdemos el control de identidad, la auditoría por usuario, los roles y la alineación con la Unidad 3 de la cátedra.
- Si solo usamos JWT, podríamos incumplir la consigna si un evaluador o cliente automatizado envía únicamente la cabecera `X-API-Key`.

## Decision
Implementar una **Estrategia de Seguridad Híbrida**:
1. **Autenticación Primaria de Usuarios (JWT Bearer)**:
   - Entidad `Usuario` con roles `Usuario` y `Administrador`.
   - Servicio `AuthService` e interfaz `IAuthService` para registro y login.
   - Hashing con PBKDF2 (`PasswordHasher<Usuario>`).
   - Clave `JWT_SECRET_KEY` cargada desde `.env` mediante `DotNetEnv`.
   - Decorar endpoints administrativos de catálogo y confirmación con `[Authorize(Roles = "Administrador")]`.
   - Configuración de seguridad Bearer en Swagger (`AddSecurityDefinition("Bearer", ...)`).
2. **Soporte de Integración Máquina a Máquina (`ApiKeyMiddleware`)**:
   - Middleware que valida la cabecera `X-API-Key` contra la variable `API_KEY_SECRET`.
   - Permite autorizar peticiones provenientes de sistemas externos (ej. subsistema hospitalario o pruebas automáticas de la consigna) que no gestionan sesiones de usuario.

## Alternatives Considered

### 1. Solo API Key (`X-API-Key`)
- **Pros**: Sencillo de implementar.
- **Cons**: No soporta roles (`Administrador` vs `Usuario`); no hay concepto de identidad de usuario; contradice la estructura pedagógica enseñada en la cátedra para autenticación backend.
- **Rejected**: Insuficiente para una API RESTful completa según la guía.

### 2. Solo JWT Bearer
- **Pros**: Máximo estándar RESTful sin estado (*stateless*); soporte nativo de Claims y Roles.
- **Cons**: Podría fallar si un script evaluador externo envía la cabecera `X-API-Key` requerida por la consigna.
- **Rejected**: Se prefiere soportar ambos para garantizar 100% de compatibilidad.

## Consequences
- **Positivas**:
  - Flexibilidad total: usuarios interactúan vía JWT y sistemas externos pueden utilizar API Key.
  - Se cumple tanto la consigna de PharmaLink como el estándar canónico de la Guía de Arquitectura.
  - Swagger UI permite autenticarse fácilmente para probar todas las operaciones.
- **Negativas / Mitigaciones**:
  - Mayor código de configuración en `Program.cs`. Mitigación: El código está claramente modularizado en secciones y documentado.
