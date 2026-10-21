using System.ComponentModel.DataAnnotations;

namespace PharmaLink.DTOs.Dispensaciones;

public class DispensacionCreateDTO
{
    [Required(ErrorMessage = "El código de receta es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El código de receta debe tener entre 3 y 100 caracteres.")]
    public string CodigoReceta { get; set; } = string.Empty;

    [Required(ErrorMessage = "El identificador del medicamento es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del medicamento debe ser mayor a 0.")]
    public int MedicamentoId { get; set; }

    [Range(1, 1000, ErrorMessage = "La cantidad a dispensar debe ser de al menos 1 y máximo 1000 unidades.")]
    public int Cantidad { get; set; }
}
