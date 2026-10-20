using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Application.Services;

namespace OrderFlow.Tests.Unit;

public class ProductServiceTests
{
    [Fact]
    public async Task Create_ThenGetById_ReturnsTheProduct()
    {
        await using var db = TestDb.Create();
        var service = new ProductService(db);

        var created = await service.CreateAsync(
            new CreateProductRequest { Name = "Keyboard", Price = 50m, StockQuantity = 5 });
        var fetched = await service.GetByIdAsync(created.Id);

        Assert.Equal("Keyboard", fetched.Name);
        Assert.Equal(5, fetched.StockQuantity);
    }

    [Fact]
    public async Task GetById_Missing_ThrowsNotFound()
    {
        await using var db = TestDb.Create();
        var service = new ProductService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAll_SearchAndPaging_ReturnsTheRightSlice()
    {
        await using var db = TestDb.Create();
        var service = new ProductService(db);
        foreach (var name in new[] { "Item1", "Item2", "Item3", "Item4", "Item5", "Other" })
            await service.CreateAsync(new CreateProductRequest { Name = name, Price = 10m, StockQuantity = 1 });

        var page = await service.GetAllAsync(new ProductQuery { Search = "Item", Page = 2, PageSize = 2 });

        Assert.Equal(5, page.TotalCount);                  // "Other" is filtered out
        Assert.Equal(3, page.TotalPages);                  // 5 items / 2 per page, rounded up
        Assert.Equal(new[] { "Item3", "Item4" }, page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task GetAll_InStockFilter_HidesSoldOutProducts()
    {
        await using var db = TestDb.Create();
        var service = new ProductService(db);
        await service.CreateAsync(new CreateProductRequest { Name = "Available", Price = 10m, StockQuantity = 3 });
        await service.CreateAsync(new CreateProductRequest { Name = "SoldOut", Price = 10m, StockQuantity = 0 });

        var page = await service.GetAllAsync(new ProductQuery { InStock = true });

        Assert.Single(page.Items);
        Assert.Equal("Available", page.Items[0].Name);
    }
}