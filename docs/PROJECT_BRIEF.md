# PharmaLink --- Resumen del Proyecto y Requisitos

## 1. Propósito del Proyecto

Desarrollar una API RESTful profesional en C# con ASP.NET Core (.NET 8/9) para la gestión integral de farmacia hospitalaria: medicamentos, insumos médicos, dispensaciones vinculadas a recetas médicas externas, reposiciones de inventario y autenticación segura.

El proyecto implementa la arquitectura canónica **MCS (Modelo - Controlador - Servicio)** descrita en la **Guía Maestra de Arquitectura Backend RESTful** de la cátedra de Programación 2 (UAP).

---

## 2. Requisitos Funcionales

1. **Gestión de Medicamentos**:
   - Listar medicamentos con filtrado en base de datos (por nombre, presentación o rango de precio).
   - Obtener detalle de medicamento por ID.
   - Crear, actualizar y eliminar medicamentos (con restricción de eliminación si existen dispensaciones históricas asociadas).
2. **Gestión de Insumos Médicos**:
   - Operaciones equivalentes a medicamentos: catálogo, consulta detallada, alta, modificación y baja.
3. **Flujo de Dispensaciones y Validación Externa**:
   - Registrar una dispensación requiriendo: `CodigoReceta`, `MedicamentoId`, `Cantidad` y `Fecha`.
   - Validar previamente el código de receta contra el servicio externo del hospital (`IHospitalApiClient`).
   - Rechazar de forma atómica si la receta no es válida/activa o si el stock del medicamento es insuficiente.
   - Si la validación es exitosa, descontar stock y persistir la dispensación en una única transacción de base de datos.
4. **Flujo de Reposiciones e Inventario**:
   - Registrar solicitudes de reposición de insumos/medicamentos.
   - Registrar confirmación o recepción de entregas, impactando el incremento de stock únicamente cuando la entrega se concreta.
   - Exponer un endpoint de resumen del flujo de reposición (solicitudes pendientes, confirmadas y balance de stock).
5. **Autenticación y Control de Acceso (Híbrido)**:
   - Registro y login de usuarios con JWT Bearer (`/api/auth/register` y `/api/auth/login`).
   - Control de roles (`Usuario` para consultas básicas, `Administrador` para mutaciones de catálogo y reposición).
   - Validación opcional de cabecera `X-API-Key` para integraciones de máquina a máquina o según la consigna.
6. **Manejo Uniforme de Respuestas y Errores**:
   - Respuestas HTTP semánticas y consistentes en JSON.
   - Manejo estructurado de errores en 3 capas.

---

## 3. Requisitos No Funcionales y Restricciones Técnicas

- **Lenguaje y Plataforma**: C# 12+ sobre .NET 8 o .NET 9 LTS.
- **Arquitectura**: MCS puro sin capa de repositorios redundante.
- **Persistencia**: Entity Framework Core con SQLite (`PharmaLink.db`).
- **Asincronía**: 100% de operaciones de I/O (base de datos y llamadas HTTP externas) implementadas con `async` / `await`.
- **Inyección de Dependencias**: Contenedor nativo de ASP.NET Core con ciclos de vida apropiados (`Scoped` para DbContext y Services; `HttpClient` tipado para servicios externos).
- **Seguridad**:
  - Contraseñas hasheadas con PBKDF2 (`PasswordHasher<T>`).
  - Clave JWT y API Keys cargadas desde `.env` (usando `DotNetEnv`) o variables de entorno del sistema.
  - No comitear nunca credenciales al control de versiones.
- **Documentación Viva**: Swagger / OpenAPI configurado en desarrollo, con soporte para autorización Bearer JWT.

---

## 4. Fuentes de Verdad y Validación de la Consigna

El equipo de desarrollo debe verificar el archivo de consigna oficial antes de asumir nombres específicos de tablas o campos externos de la receta hospitalaria. Si algún detalle no está especificado en la consigna, se debe formular una consulta o registrar la decisión tomada en un ADR dentro de `docs/decisions/`.
