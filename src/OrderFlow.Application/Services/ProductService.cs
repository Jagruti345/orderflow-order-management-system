using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;

    public ProductService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query, CancellationToken ct = default)
    {
        var q = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(p => p.Name.Contains(term));      // becomes SQL: WHERE Name LIKE '%term%'
        }

        if (query.InStock == true)
            q = q.Where(p => p.StockQuantity > 0);

        var total = await q.CountAsync(ct);               // total BEFORE paging, so clients can build page links

        var items = await q
            .OrderBy(p => p.Name).ThenBy(p => p.Id)       // paging needs a stable, unique order
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.StockQuantity))
            .ToListAsync(ct);

        return new PagedResult<ProductDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException($"Product {id} was not found.");
        return ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product(request.Name, request.Description ?? string.Empty,
                                  request.Price, request.StockQuantity);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException($"Product {id} was not found.");

        product.UpdateDetails(request.Name, request.Description ?? string.Empty, request.Price);
        await _db.SaveChangesAsync(ct);
        return ToDto(product);
    }

    private static ProductDto ToDto(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.StockQuantity);
}