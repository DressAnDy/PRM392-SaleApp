using Microsoft.AspNetCore.Mvc;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;

namespace SaleApp.API.Controllers;

/// <summary>
/// Store Location API endpoints
/// Provides endpoints to view store locations on a map
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly IStoreLocationService _storeLocationService;
    private readonly ILogger<StoresController> _logger;

    public StoresController(
        IStoreLocationService storeLocationService,
        ILogger<StoresController> logger)
    {
        _storeLocationService = storeLocationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all store locations
    /// </summary>
    /// <returns>List of all store locations for displaying on map</returns>
    /// <response code="200">Returns list of all stores</response>
    /// <response code="500">Server error occurred</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StoreLocationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllStores()
    {
        try
        {
            _logger.LogInformation("Getting all store locations");

            var stores = await _storeLocationService.GetAllStoresAsync();

            return Ok(new ApiResponse<List<StoreLocationDto>>
            {
                Success = true,
                Message = "Get all store locations successfully",
                Data = stores
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all stores");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorApiResponse
                {
                    Success = false,
                    Message = "Error retrieving store locations"
                });
        }
    }

    /// <summary>
    /// Get store details by ID
    /// </summary>
    /// <param name="id">Store location ID</param>
    /// <returns>Store location details</returns>
    /// <response code="200">Returns store details</response>
    /// <response code="404">Store not found</response>
    /// <response code="500">Server error occurred</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<StoreLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStoreById(int id)
    {
        try
        {
            _logger.LogInformation("Getting store location with ID: {StoreId}", id);

            var store = await _storeLocationService.GetStoreByIdAsync(id);

            if (store == null)
            {
                return NotFound(new ErrorApiResponse
                {
                    Success = false,
                    Message = "Store location not found"
                });
            }

            return Ok(new ApiResponse<StoreLocationDto>
            {
                Success = true,
                Message = "Get store location details successfully",
                Data = store
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store with ID: {StoreId}", id);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorApiResponse
                {
                    Success = false,
                    Message = "Error retrieving store location"
                });
        }
    }

    /// <summary>
    /// Search stores near a specific location
    /// </summary>
    /// <param name="latitude">User's current latitude (required)</param>
    /// <param name="longitude">User's current longitude (required)</param>
    /// <param name="radius">Search radius in kilometers (optional, default: 5)</param>
    /// <returns>List of stores within the specified radius, sorted by distance</returns>
    /// <response code="200">Returns list of nearby stores</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Server error occurred</response>
    [HttpGet("search/nearby")]
    [ProducesResponseType(typeof(ApiResponse<List<StoreLocationWithDistanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchNearby(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery(Name = "radius")] int radius = 5)
    {
        try
        {
            // Validate coordinates
            if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            {
                return BadRequest(new ErrorApiResponse
                {
                    Success = false,
                    Message = "Invalid latitude or longitude values"
                });
            }

            // Validate radius
            if (radius <= 0)
            {
                return BadRequest(new ErrorApiResponse
                {
                    Success = false,
                    Message = "Radius must be greater than 0"
                });
            }

            _logger.LogInformation(
                "Searching nearby stores - Latitude: {Latitude}, Longitude: {Longitude}, Radius: {Radius}",
                latitude, longitude, radius);

            var nearbyStores = await _storeLocationService.SearchStoresNearbyAsync(latitude, longitude, radius);

            return Ok(new ApiResponse<List<StoreLocationWithDistanceDto>>
            {
                Success = true,
                Message = "Search nearby stores successfully",
                Data = nearbyStores
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching nearby stores");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorApiResponse
                {
                    Success = false,
                    Message = "Error searching nearby stores"
                });
        }
    }
}

/// <summary>
/// Generic API response wrapper for successful responses
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

/// <summary>
/// API response wrapper for error responses
/// </summary>
public class ErrorApiResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
}
