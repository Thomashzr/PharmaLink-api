namespace PharmaLink.DTOs.Reposiciones;

public class InsumoStockDTO
{
    public int InsumoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int StockActual { get; set; }
}
