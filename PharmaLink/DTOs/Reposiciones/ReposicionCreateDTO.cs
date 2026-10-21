using System.ComponentModel.DataAnnotations;

namespace PharmaLink.DTOs.Reposiciones;

public class ReposicionCreateDTO
{
    [Required(ErrorMessage = "Debe especificar los ítems a reponer.")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un insumo en la solicitud de reposición.")]
    public List<EntregaItemDTO> Items { get; set; } = new();
}
