using System.ComponentModel.DataAnnotations;

namespace PharmaLink.DTOs.Insumos;

public class InsumoCreateDTO
{
    [Required(ErrorMessage = "El nombre del insumo es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La presentación es obligatoria.")]
    [StringLength(100, ErrorMessage = "La presentación no puede exceder 100 caracteres.")]
    public string Presentacion { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "El stock inicial no puede ser negativo ni exceder 100.000 unidades.")]
    public int StockDisponible { get; set; }

    [Range(0.01, 1000000.0, ErrorMessage = "El precio unitario debe ser mayor a 0.")]
    public decimal PrecioUnitario { get; set; }
}
