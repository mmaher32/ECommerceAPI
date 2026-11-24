using ECommerce.Domain.Entities;
using ECommerce.Domain.Errors;

namespace ECommerce.UnitTests.Domain;

public class ProductTests
{
    private static readonly Guid BrandId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_TrimsAndSetsFields()
    {
        var result = Product.Create("  Boots  ", " Warm ", " /img.png ", 49.99m, BrandId, TypeId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Boots", result.Value.Name);
        Assert.Equal("Warm", result.Value.Description);
        Assert.Equal("/img.png", result.Value.PictureUrl);
        Assert.Equal(49.99m, result.Value.Price);
        Assert.Equal(BrandId, result.Value.ProductBrandId);
        Assert.Equal(TypeId, result.Value.ProductTypeId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositivePrice_Fails(decimal price)
    {
        var result = Product.Create("Boots", "Warm", "/img.png", price, BrandId, TypeId);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InvalidPrice, result.Error);
    }

    [Fact]
    public void Create_WithNameTooLong_Fails()
    {
        var name = new string('a', Product.MaxNameLength + 1);

        var result = Product.Create(name, "Warm", "/img.png", 10m, BrandId, TypeId);

        Assert.Equal(ProductErrors.NameTooLong, result.Error);
    }

    [Fact]
    public void Create_WithEmptyBrand_Fails()
    {
        var result = Product.Create("Boots", "Warm", "/img.png", 10m, Guid.Empty, TypeId);

        Assert.Equal(ProductErrors.BrandRequired, result.Error);
    }

    [Fact]
    public void Update_WithInvalidPrice_Fails()
    {
        var product = Product.Create("Boots", "Warm", "/img.png", 10m, BrandId, TypeId).Value;

        var result = product.Update("Boots", "Warm", "/img.png", 0m, BrandId, TypeId);

        Assert.Equal(ProductErrors.InvalidPrice, result.Error);
    }

    [Fact]
    public void MarkAsDeleted_SetsFlag()
    {
        var product = Product.Create("Boots", "Warm", "/img.png", 10m, BrandId, TypeId).Value;

        product.MarkAsDeleted();

        Assert.True(product.IsDeleted);
    }
}
