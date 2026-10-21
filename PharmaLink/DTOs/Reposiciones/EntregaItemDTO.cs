using System.ComponentModel.DataAnnotations;

namespace PharmaLink.DTOs.Reposiciones;

public class EntregaItemDTO
{
    [Required(ErrorMessage = "El ID del insumo es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del insumo debe ser mayor a 0.")]
    public int InsumoId { get; set; }

    [Range(1, 50000, ErrorMessage = "La cantidad a reponer debe ser de al menos 1 y máximo 50.000 unidades.")]
    public int Cantidad { get; set; }
}
