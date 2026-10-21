namespace PharmaLink.DTOs.Reposiciones;

public class EntregaDetalleDTO
{
    public int Id { get; set; }
    public int InsumoId { get; set; }
    public string NombreInsumo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime FechaEntrega { get; set; }
}
