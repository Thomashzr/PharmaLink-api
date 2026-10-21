namespace PharmaLink.DTOs.Reposiciones;

public class ResumenReposicionDTO
{
    public int TotalSolicitudes { get; set; }
    public int TotalConfirmadas { get; set; }
    public int TotalPendientes { get; set; }
    public List<InsumoStockDTO> StockInsumos { get; set; } = new();
}
