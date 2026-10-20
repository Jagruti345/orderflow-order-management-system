using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Application.Services;

namespace OrderFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    private readonly IOutputCacheStore _cache;

    public OrdersController(IOrderService service, IOutputCacheStore cache)
    {
        _service = service;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetAll([FromQuery] OrderQuery query, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        await _cache.EvictByTagAsync("products", ct);   // stock changed
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/pay")]
    public async Task<ActionResult<OrderDto>> Pay(int id, CancellationToken ct) =>
        Ok(await _service.PayAsync(id, ct));

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/ship")]
    public async Task<ActionResult<OrderDto>> Ship(int id, CancellationToken ct) =>
        Ok(await _service.ShipAsync(id, ct));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(int id, CancellationToken ct)
    {
        var cancelled = await _service.CancelAsync(id, ct);
        await _cache.EvictByTagAsync("products", ct);   // stock was restored
        return Ok(cancelled);
    }
}