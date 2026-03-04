namespace SaleApp.Domain.Entities;

public class StoreLocation
{
    public int LocationId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
}
