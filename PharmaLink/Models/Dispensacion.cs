using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaLink.Models;

public class Dispensacion
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string CodigoReceta { get; set; } = string.Empty;

    public int MedicamentoId { get; set; }

    [ForeignKey(nameof(MedicamentoId))]
    public Medicamento? Medicamento { get; set; }

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; }

    public DateTime FechaDispensacion { get; set; } = DateTime.UtcNow;
}
