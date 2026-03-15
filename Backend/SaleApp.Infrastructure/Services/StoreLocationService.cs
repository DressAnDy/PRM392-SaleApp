using Microsoft.EntityFrameworkCore;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using SaleApp.Infrastructure.Data;

namespace SaleApp.Infrastructure.Services;

public class StoreLocationService : IStoreLocationService
{
    private readonly SaleAppDbContext _context;

    public StoreLocationService(SaleAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<StoreLocationDto>> GetAllStoresAsync()
    {
        try
        {
            var stores = await _context.StoreLocations
                .AsNoTracking()
                .Select(s => MapToDto(s))
                .ToListAsync();

            return stores;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error retrieving store locations", ex);
        }
    }

    public async Task<StoreLocationDto?> GetStoreByIdAsync(int locationId)
    {
        try
        {
            var store = await _context.StoreLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.LocationId == locationId);

            return store != null ? MapToDto(store) : null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error retrieving store location with ID {locationId}", ex);
        }
    }

    public async Task<List<StoreLocationWithDistanceDto>> SearchStoresNearbyAsync(
        decimal latitude,
        decimal longitude,
        int radiusKm = 5)
    {
        try
        {
            var stores = await _context.StoreLocations
                .AsNoTracking()
                .ToListAsync();

            var storesWithDistance = stores
                .Select(s => new
                {
                    Store = s,
                    Distance = CalculateDistance((double)latitude, (double)longitude, (double)s.Latitude, (double)s.Longitude)
                })
                .Where(x => x.Distance <= radiusKm)
                .OrderBy(x => x.Distance)
                .Select(x => new StoreLocationWithDistanceDto
                {
                    LocationId = x.Store.LocationId,
                    Address = x.Store.Address,
                    Latitude = x.Store.Latitude,
                    Longitude = x.Store.Longitude,
                    Distance = Math.Round(x.Distance, 2)
                })
                .ToList();

            return storesWithDistance;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error searching nearby store locations", ex);
        }
    }

    /// <summary>
    /// Calculate distance between two points using Haversine formula
    /// Returns distance in kilometers
    /// </summary>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0; // Earth's radius in kilometers

        var dLat = DegToRad(lat2 - lat1);
        var dLon = DegToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegToRad(lat1)) * Math.Cos(DegToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = earthRadiusKm * c;

        return distance;
    }

    private static double DegToRad(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static StoreLocationDto MapToDto(SaleApp.Domain.Entities.StoreLocation store)
    {
        return new StoreLocationDto
        {
            LocationId = store.LocationId,
            Address = store.Address,
            Latitude = store.Latitude,
            Longitude = store.Longitude
        };
    }
}
