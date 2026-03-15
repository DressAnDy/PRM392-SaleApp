using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

public interface IStoreLocationService
{
    /// <summary>
    /// Get all store locations
    /// </summary>
    /// <returns>List of all store locations</returns>
    Task<List<StoreLocationDto>> GetAllStoresAsync();

    /// <summary>
    /// Get a specific store by ID
    /// </summary>
    /// <param name="locationId">Store location ID</param>
    /// <returns>Store location details or null if not found</returns>
    Task<StoreLocationDto?> GetStoreByIdAsync(int locationId);

    /// <summary>
    /// Search stores within a radius from a given location
    /// </summary>
    /// <param name="latitude">User's latitude</param>
    /// <param name="longitude">User's longitude</param>
    /// <param name="radiusKm">Search radius in kilometers (default: 5)</param>
    /// <returns>List of stores within the radius, sorted by distance</returns>
    Task<List<StoreLocationWithDistanceDto>> SearchStoresNearbyAsync(
        decimal latitude,
        decimal longitude,
        int radiusKm = 5);
}
