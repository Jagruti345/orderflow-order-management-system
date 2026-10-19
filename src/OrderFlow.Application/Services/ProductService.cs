using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;

    public ProductService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()                       // read-only query: faster
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.StockQuantity))
            .ToListAsync(ct);
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
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)   // tracked
            ?? throw new NotFoundException($"Product {id} was not found.");

        product.UpdateDetails(request.Name, request.Description ?? string.Empty, request.Price);
        await _db.SaveChangesAsync(ct);   // EF detects the change and writes the UPDATE
        return ToDto(product);
    }

    private static ProductDto ToDto(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.StockQuantity);
}