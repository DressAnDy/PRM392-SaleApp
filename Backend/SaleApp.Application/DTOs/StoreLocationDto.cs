namespace SaleApp.Application.DTOs;

/// <summary>
/// DTO for StoreLocation entity
/// </summary>
public class StoreLocationDto
{
    public int LocationId { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

/// <summary>
/// Extended DTO with distance information for search results
/// </summary>
public class StoreLocationWithDistanceDto : StoreLocationDto
{
    public double? Distance { get; set; } // Distance in kilometers from search location
}
