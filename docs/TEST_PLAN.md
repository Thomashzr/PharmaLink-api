# Plan de Pruebas y Criterios de Aceptación --- PharmaLink

Este documento define la estrategia de pruebas unitarias y de integración para garantizar la robustez funcional y el cumplimiento arquitectónico de PharmaLink.

---

## 1. Estrategia de Testing

Se implementan dos niveles de pruebas:
1. **Pruebas Unitarias (`PharmaLink.Tests/Unit/`)**:
   - Prueban la lógica de negocio aislada en `Services/`.
   - Utilizan el proveedor `Microsoft.EntityFrameworkCore.InMemory` o una base SQLite en memoria para aislar la persistencia.
   - Utilizan `Moq` o `NSubstitute` para simular llamadas al cliente externo `IHospitalApiClient` y al logger.
2. **Pruebas de Integración (`PharmaLink.Tests/Integration/`)**:
   - Utilizan `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`).
   - Verifican el pipeline HTTP completo: serialización de DTOs, validaciones `[ApiController]`, middlewares de autenticación JWT y códigos de estado devueltos.

---

## 2. Casos de Prueba Críticos Obligatorios

| Identificador | Componente | Escenario de Prueba | Resultado Esperado |
| :--- | :--- | :--- | :--- |
| **UT-AUTH-01** | `AuthService` | Registro de usuario con nombre no existente | Retorna `true` y persiste el usuario con contraseña hasheada. |
| **UT-AUTH-02** | `AuthService` | Registro con nombre de usuario duplicado | Retorna `false` (conflicto). |
| **UT-AUTH-03** | `AuthService` | Login con credenciales válidas | Retorna `AuthResponseDTO` con token JWT válido y roles. |
| **UT-AUTH-04** | `AuthService` | Login con contraseña incorrecta | Retorna `null` (`401 Unauthorized`). |
| **UT-MED-01** | `MedicamentoDbService` | Búsqueda con filtro parcial de nombre | Retorna únicamente los medicamentos que coinciden, proyectados en `MedicamentoReadDTO`. |
| **UT-MED-02** | `MedicamentoDbService` | Eliminación de medicamento sin dispensaciones | Retorna `true` y elimina la entidad de la base de datos. |
| **UT-MED-03** | `MedicamentoDbService` | Eliminación de medicamento con dispensaciones | Lanza excepción controlada o retorna `false`, impidiendo la eliminación (integridad referencial). |
| **UT-DISP-01** | `DispensacionDbService`| Receta válida y stock suficiente | Decrementa el stock del medicamento, crea la dispensación y retorna DTO con `201`. |
| **UT-DISP-02** | `DispensacionDbService`| Receta rechazada por el hospital | Lanza `InvalidOperationException` y el stock permanece intacto (Rollback). |
| **UT-DISP-03** | `DispensacionDbService`| Stock insuficiente para la cantidad solicitada | Lanza `InvalidOperationException` informando el faltante y aborta la transacción. |
| **UT-DISP-04** | `DispensacionDbService`| Error de conexión con la API del hospital | Se propaga `HttpRequestException` para que el middleware responda `502 Bad Gateway`. |
| **UT-REPO-01** | `ReposicionDbService`  | Crear orden de reposición | Estado inicial `Solicitada`; el stock de insumos NO se modifica. |
| **UT-REPO-02** | `ReposicionDbService`  | Confirmar entrega de reposición | Cambia estado a `Confirmada` e incrementa el stock de cada insumo en la cantidad entregada. |
| **UT-REPO-03** | `ReposicionDbService`  | Intentar reconfirmar una reposición ya confirmada | Lanza excepción de negocio impidiendo la doble acreditación de stock. |

---

## 3. Ejemplo de Test Unitario con Mocking y SQLite en Memoria

```csharp
[Fact]
public async Task RegistrarDispensacionAsync_RecetaValidaYStockSuficiente_DebeDecrementarStockYGuardar()
{
    // Arrange: Configurar BD en memoria y Mocks
    var options = new DbContextOptionsBuilder<PharmaLinkContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    using var context = new PharmaLinkContext(options);
    var medicamento = new Medicamento 
    { 
        Id = 1, 
        Nombre = "Amoxicilina 500mg", 
        Presentacion = "Cápsulas", 
        StockDisponible = 10, 
        PrecioUnitario = 1500m 
    };
    context.Medicamentos.Add(medicamento);
    await context.SaveChangesAsync();

    var mockHospital = new Mock<IHospitalApiClient>();
    mockHospital.Setup(h => h.ValidarRecetaAsync("REC-123", 1, 3))
        .ReturnsAsync(true);

    var mockLogger = new Mock<ILogger<DispensacionDbService>>();
    var servicio = new DispensacionDbService(context, mockHospital.Object, mockLogger.Object);

    var dto = new DispensacionCreateDTO { CodigoReceta = "REC-123", MedicamentoId = 1, Cantidad = 3 };

    // Act
    var resultado = await servicio.RegistrarDispensacionAsync(dto);

    // Assert
    Assert.NotNull(resultado);
    Assert.Equal("REC-123", resultado.CodigoReceta);
    Assert.Equal(3, resultado.Cantidad);

    var medicamentoActualizado = await context.Medicamentos.FindAsync(1);
    Assert.Equal(7, medicamentoActualizado!.StockDisponible); // 10 - 3 = 7
}
```

---

## 4. Criterios de Aceptación para Dar por Terminado el Proyecto

El proyecto se considera completado satisfactoriamente cuando se verifique:
1. `dotnet build` finaliza con **0 errores y 0 advertencias**.
2. `dotnet test` ejecuta la suite de pruebas unitarias e integración con **100% de tests aprobados**.
3. Swagger UI está accesible en `/swagger` en entorno Development y permite autenticarse con token Bearer.
4. Las contraseñas se almacenan con PBKDF2 en la base de datos local SQLite.
5. Los archivos de secretos (`.env`, `*.db`) están excluidos en `.gitignore`.
