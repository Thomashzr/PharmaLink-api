namespace PharmaLink.DTOs.Insumos;

public class InsumoReadDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Presentacion { get; set; } = string.Empty;
    public int StockDisponible { get; set; }
    public decimal PrecioUnitario { get; set; }
}
