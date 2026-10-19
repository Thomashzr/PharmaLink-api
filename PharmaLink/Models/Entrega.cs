using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaLink.Models;

public class Entrega
{
    public int Id { get; set; }

    public int ReposicionId { get; set; }

    [ForeignKey(nameof(ReposicionId))]
    public Reposicion? Reposicion { get; set; }

    public int InsumoId { get; set; }

    [ForeignKey(nameof(InsumoId))]
    public Insumo? Insumo { get; set; }

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; }

    public DateTime FechaEntrega { get; set; } = DateTime.UtcNow;
}
