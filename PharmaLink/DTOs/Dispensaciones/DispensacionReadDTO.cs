namespace PharmaLink.DTOs.Dispensaciones;

public class DispensacionReadDTO
{
    public int Id { get; set; }
    public string CodigoReceta { get; set; } = string.Empty;
    public int MedicamentoId { get; set; }
    public string NombreMedicamento { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime FechaDispensacion { get; set; }
}
