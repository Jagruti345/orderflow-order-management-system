using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Tests.Unit;

public class ProductTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99.5)]
    public void Constructor_InvalidPrice_Throws(decimal price)
    {
        Assert.Throws<DomainException>(() => new Product("Pen", "", price, 5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankName_Throws(string name)
    {
        Assert.Throws<DomainException>(() => new Product(name, "", 10m, 5));
    }

    [Fact]
    public void DecreaseStock_MoreThanAvailable_ThrowsAndKeepsStock()
    {
        var product = new Product("Mouse", "", 20m, 2);

        Assert.Throws<DomainException>(() => product.DecreaseStock(3));
        Assert.Equal(2, product.StockQuantity);   // a failed operation must not change anything
    }
}