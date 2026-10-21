namespace PharmaLink.DTOs.Reposiciones;

public class ReposicionReadDTO
{
    public int Id { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public List<EntregaDetalleDTO> Entregas { get; set; } = new();
}
