using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Dtos;
using OrderFlow.Application.Services;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);   // 201 + Location header
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(id, request, ct));
}