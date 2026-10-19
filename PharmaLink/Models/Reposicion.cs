namespace PharmaLink.Models;

public enum EstadoReposicion
{
    Solicitada = 1,
    Confirmada = 2,
    Cancelada = 3
}

public class Reposicion
{
    public int Id { get; set; }

    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

    public DateTime? FechaConfirmacion { get; set; }

    public EstadoReposicion Estado { get; set; } = EstadoReposicion.Solicitada;

    // Colección de insumos/detalles entregados en esta reposición
    public List<Entrega> Entregas { get; set; } = new();
}
