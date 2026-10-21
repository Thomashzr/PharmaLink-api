namespace PharmaLink.DTOs.External;

public class RecetaValidacionResultDTO
{
    public bool EsValida { get; set; }
    public string CodigoReceta { get; set; } = string.Empty;
    public int? MedicamentoId { get; set; }
    public int? CantidadPrescripta { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
