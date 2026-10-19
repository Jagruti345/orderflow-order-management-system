using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Dtos;
using OrderFlow.Application.Services;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}