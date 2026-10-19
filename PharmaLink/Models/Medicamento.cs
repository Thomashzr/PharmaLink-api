using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaLink.Models;

public class Medicamento
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Presentacion { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int StockDisponible { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.0, 1000000.0)]
    public decimal PrecioUnitario { get; set; }

    // Propiedad de navegación
    public List<Dispensacion> Dispensaciones { get; set; } = new();
}
