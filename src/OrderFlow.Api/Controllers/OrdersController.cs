using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Dtos;
using OrderFlow.Application.Services;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll([FromQuery] OrderStatus? status, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(status, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/pay")]
    public async Task<ActionResult<OrderDto>> Pay(int id, CancellationToken ct) =>
        Ok(await _service.PayAsync(id, ct));

    [HttpPost("{id:int}/ship")]
    public async Task<ActionResult<OrderDto>> Ship(int id, CancellationToken ct) =>
        Ok(await _service.ShipAsync(id, ct));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(int id, CancellationToken ct) =>
        Ok(await _service.CancelAsync(id, ct));
}