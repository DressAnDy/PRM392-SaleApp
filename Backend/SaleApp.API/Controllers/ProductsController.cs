using Microsoft.AspNetCore.Mvc;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;

namespace SaleApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Get all products with basic information (name, image, price, description).
    /// </summary>
    [HttpGet("getall")]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    /// <summary>
    /// Get a paged list of products with sorting and filtering.
    /// sortBy: newest | price_asc | price_desc | popularity
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductQueryDto query)
    {
        var result = await _productService.GetProductsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Get full product detail including reviews.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found." });

        return Ok(product);
    }
}
