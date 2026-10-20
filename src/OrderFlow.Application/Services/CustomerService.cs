using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetAllAsync(CustomerQuery query, CancellationToken ct = default);
    Task<CustomerDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    
}

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _db;

    public CustomerService(IApplicationDbContext db) => _db = db;

   public async Task<PagedResult<CustomerDto>> GetAllAsync(CustomerQuery query, CancellationToken ct = default)
{
    var q = _db.Customers.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(query.Search))
    {
        var term = query.Search.Trim();
        q = q.Where(c => c.FullName.Contains(term) || c.Email.Contains(term));
    }

    var total = await q.CountAsync(ct);

    var items = await q
        .OrderBy(c => c.FullName).ThenBy(c => c.Id)
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
        .Select(c => new CustomerDto(c.Id, c.FullName, c.Email, c.Phone))
        .ToListAsync(ct);

    return new PagedResult<CustomerDto>(items, query.Page, query.PageSize, total);
}
    public async Task<CustomerDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"Customer {id} was not found.");
        return ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = new Customer(request.FullName, request.Email, request.Phone);

        // Friendly check; the unique index in the database is the real safety net
        if (await _db.Customers.AnyAsync(c => c.Email == customer.Email, ct))
            throw new ConflictException($"A customer with email '{customer.Email}' already exists.");

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return ToDto(customer);
    }

    private static CustomerDto ToDto(Customer c) => new(c.Id, c.FullName, c.Email, c.Phone);
}