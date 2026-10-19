# Contrato de API RESTful --- PharmaLink

Este documento define la especificación canónica de endpoints, contratos DTO de entrada/salida, códigos de estado HTTP y políticas de seguridad para PharmaLink.

---

## 1. Convenciones RESTful Generales

1. **Ruta Base**: `/api/[recurso]` en minúsculas y sustantivos en plural (`/api/medicamentos`, `/api/insumos`).
2. **Formato**: JSON en cuerpos de petición y respuesta (`Content-Type: application/json`).
3. **Semántica de Verbos**:
   - `GET`: Lectura idempotente. Parámetros de filtrado mediante *Query String*.
   - `POST`: Creación de recurso o ejecución de operación transaccional. Devuelve `201 Created` con cabecera `Location`.
   - `PUT`: Modificación total del recurso. Idempotente. Devuelve `204 No Content` o `200 OK`.
   - `DELETE`: Eliminación de recurso. Devuelve `204 No Content`.
4. **Respuestas de Error Uniformes**:
   - `400 Bad Request`: Validación de modelo fallida (campos obligatorios, longitudes, rangos).
   - `401 Unauthorized`: Token JWT ausente, expirado o API Key inválida.
   - `403 Forbidden`: Usuario autenticado pero sin rol adecuado (`Administrador`).
   - `404 Not Found`: Recurso inexistente con cuerpo `{ "mensaje": "..." }`.
   - `409 Conflict`: Conflicto de negocio (stock insuficiente, duplicados, estado incompatible).
   - `502 Bad Gateway`: Falla de conectividad o respuesta anómala de la API hospitalaria externa.

---

## 2. Catálogo de Endpoints

### 2.1 Autenticación (`/api/auth`)

| Verbo | Ruta | Seguridad | Propósito | Código Éxito |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Anónimo | Registro de nuevo usuario (Rol por defecto: Usuario) | `200 OK` |
| `POST` | `/api/auth/login` | Anónimo | Autenticación y obtención de Token JWT | `200 OK` |

### 2.2 Medicamentos (`/api/medicamentos`)

| Verbo | Ruta | Seguridad | Propósito | Código Éxito |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/medicamentos?search=ibu&page=1` | Anónimo / Usuario | Listar medicamentos con filtrado en base de datos | `200 OK` |
| `GET` | `/api/medicamentos/{id}` | Anónimo / Usuario | Obtener detalle de medicamento por ID | `200 OK` |
| `POST` | `/api/medicamentos` | `Authorize(Roles = "Administrador")` | Crear un nuevo medicamento | `201 Created` |
| `PUT` | `/api/medicamentos/{id}` | `Authorize(Roles = "Administrador")` | Actualizar medicamento existente | `204 No Content` |
| `DELETE`| `/api/medicamentos/{id}` | `Authorize(Roles = "Administrador")` | Eliminar medicamento (si no tiene dispensaciones) | `204 No Content` |

### 2.3 Insumos (`/api/insumos`)

| Verbo | Ruta | Seguridad | Propósito | Código Éxito |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/insumos?search=guantes` | Anónimo / Usuario | Listar insumos con filtrado | `200 OK` |
| `GET` | `/api/insumos/{id}` | Anónimo / Usuario | Obtener detalle de insumo | `200 OK` |
| `POST` | `/api/insumos` | `Authorize(Roles = "Administrador")` | Crear nuevo insumo médico | `201 Created` |
| `PUT` | `/api/insumos/{id}` | `Authorize(Roles = "Administrador")` | Actualizar insumo | `204 No Content` |
| `DELETE`| `/api/insumos/{id}` | `Authorize(Roles = "Administrador")` | Eliminar insumo | `204 No Content` |

### 2.4 Dispensaciones (`/api/dispensaciones`)

| Verbo | Ruta | Seguridad | Propósito | Código Éxito |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/dispensaciones` | `Authorize` / API Key | Validar receta contra hospital, verificar stock y registrar | `201 Created` |
| `GET` | `/api/dispensaciones/{id}` | `Authorize` | Consultar registro de dispensación con medicamento | `200 OK` |

### 2.5 Reposiciones (`/api/reposiciones`)

| Verbo | Ruta | Seguridad | Propósito | Código Éxito |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/reposiciones` | `Authorize(Roles = "Administrador")` | Crear solicitud de reposición de insumos | `201 Created` |
| `GET` | `/api/reposiciones/{id}` | `Authorize` | Consultar orden de reposición con sus entregas | `200 OK` |
| `POST` | `/api/reposiciones/{id}/confirmar` | `Authorize(Roles = "Administrador")` | Confirmar recepción de entrega e incrementar stock | `200 OK` |
| `GET` | `/api/reposiciones/resumen` | `Authorize` | Resumen de solicitudes, entregas y stock actualizado | `200 OK` |

---

## 3. Especificación de Contratos DTOs (`DTOs/`)

### 3.1 DTOs de Autenticación
```csharp
namespace PharmaLink.DTOs.Auth;

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

### 3.2 DTOs de Medicamentos
```csharp
namespace PharmaLink.DTOs.Medicamentos;

public class MedicamentoCreateDTO
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Presentacion { get; set; } = string.Empty;

    [Range(0, 100000)]
    public int StockDisponible { get; set; }

    [Range(0.01, 1000000.0)]
    public decimal PrecioUnitario { get; set; }
}

public class MedicamentoUpdateDTO
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Presentacion { get; set; } = string.Empty;

    [Range(0, 100000)]
    public int StockDisponible { get; set; }

    [Range(0.01, 1000000.0)]
    public decimal PrecioUnitario { get; set; }
}

public class MedicamentoReadDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Presentacion { get; set; } = string.Empty;
    public int StockDisponible { get; set; }
    public decimal PrecioUnitario { get; set; }
}
```

### 3.3 DTOs de Insumos
```csharp
namespace PharmaLink.DTOs.Insumos;

public class InsumoCreateDTO
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Presentacion { get; set; } = string.Empty;

    [Range(0, 100000)]
    public int StockDisponible { get; set; }

    [Range(0.01, 1000000.0)]
    public decimal PrecioUnitario { get; set; }
}

public class InsumoReadDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Presentacion { get; set; } = string.Empty;
    public int StockDisponible { get; set; }
    public decimal PrecioUnitario { get; set; }
}
```

### 3.4 DTOs de Dispensaciones
```csharp
namespace PharmaLink.DTOs.Dispensaciones;

public class DispensacionCreateDTO
{
    [Required, StringLength(100)]
    public string CodigoReceta { get; set; } = string.Empty;

    [Required]
    public int MedicamentoId { get; set; }

    [Range(1, 1000, ErrorMessage = "La cantidad a dispensar debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}

public class DispensacionReadDTO
{
    public int Id { get; set; }
    public string CodigoReceta { get; set; } = string.Empty;
    public int MedicamentoId { get; set; }
    public string NombreMedicamento { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime FechaDispensacion { get; set; }
}
```

### 3.5 DTOs de Reposiciones y Resumen
```csharp
namespace PharmaLink.DTOs.Reposiciones;

public class EntregaItemDTO
{
    [Required]
    public int InsumoId { get; set; }

    [Range(1, 50000)]
    public int Cantidad { get; set; }
}

public class ReposicionCreateDTO
{
    [Required, MinLength(1, ErrorMessage = "Debe incluir al menos un insumo en la reposición.")]
    public List<EntregaItemDTO> Items { get; set; } = new();
}

public class ReposicionReadDTO
{
    public int Id { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public List<EntregaDetalleDTO> Entregas { get; set; } = new();
}

public class EntregaDetalleDTO
{
    public int Id { get; set; }
    public int InsumoId { get; set; }
    public string NombreInsumo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime FechaEntrega { get; set; }
}

public class ResumenReposicionDTO
{
    public int TotalSolicitudes { get; set; }
    public int TotalConfirmadas { get; set; }
    public int TotalPendientes { get; set; }
    public List<InsumoStockDTO> StockInsumos { get; set; } = new();
}

public class InsumoStockDTO
{
    public int InsumoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int StockActual { get; set; }
}
```

---

## 4. Ejemplos de Request / Response

### Registrar Dispensación (`POST /api/dispensaciones`)
**Request Body**:
```json
{
  "codigoReceta": "REC-2026-9812",
  "medicamentoId": 3,
  "cantidad": 2
}
```
**Respuesta Exitosa (`201 Created`)**:
*Headers*: `Location: /api/dispensaciones/14`
```json
{
  "id": 14,
  "codigoReceta": "REC-2026-9812",
  "medicamentoId": 3,
  "nombreMedicamento": "Paracetamol 500mg",
  "cantidad": 2,
  "fechaDispensacion": "2026-10-09T14:30:00Z"
}
```
**Respuesta con Receta Inválida (`400 Bad Request` o `409 Conflict`)**:
```json
{
  "mensaje": "La receta REC-2026-9812 no se encuentra activa o no autoriza el medicamento solicitado."
}
```
**Respuesta con Stock Insuficiente (`409 Conflict`)**:
```json
{
  "mensaje": "Stock insuficiente para el medicamento Paracetamol 500mg. Stock disponible: 1, solicitado: 2."
}
```
