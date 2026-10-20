using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Application.Services;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly IOutputCacheStore _cache;

    public ProductsController(IProductService service, IOutputCacheStore cache)
    {
        _service = service;
        _cache = cache;
    }

    [AllowAnonymous]
    [HttpGet]
    [OutputCache(PolicyName = "Catalog")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll([FromQuery] ProductQuery query, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(query, ct));

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [OutputCache(PolicyName = "Catalog")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(id, ct));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        await _cache.EvictByTagAsync("products", ct);   // cached lists are now stale
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, request, ct);
        await _cache.EvictByTagAsync("products", ct);
        return Ok(updated);
    }
}