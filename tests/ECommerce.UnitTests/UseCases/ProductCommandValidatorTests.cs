using ECommerce.Domain.Entities;
using ECommerce.UseCases.Products.Commands.CreateProduct;
using ECommerce.UseCases.Products.Commands.UpdateProduct;
using ECommerce.UseCases.Products.Commands.Validators;

namespace ECommerce.UnitTests.UseCases;

public class ProductCommandValidatorTests
{
    private static CreateProductCommand ValidCreate() =>
        new("Boots", "Warm", "/img.png", 10m, Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Create_ValidCommand_Passes()
    {
        var result = new CreateProductCommandValidator().Validate(ValidCreate());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_ReportsEveryInvalidField()
    {
        var command = new CreateProductCommand("", "", "", 0m, Guid.Empty, Guid.Empty);

        var result = new CreateProductCommandValidator().Validate(command);

        var codes = result.Errors.Select(e => e.ErrorCode).ToHashSet();
        Assert.Contains("Product.Name.Required", codes);
        Assert.Contains("Product.Description.Required", codes);
        Assert.Contains("Product.PictureUrl.Required", codes);
        Assert.Contains("Product.Price.Invalid", codes);
        Assert.Contains("Product.Brand.Required", codes);
        Assert.Contains("Product.Type.Required", codes);
    }

    [Fact]
    public void Create_NameTooLong_Fails()
    {
        var command = ValidCreate() with { Name = new string('a', Product.MaxNameLength + 1) };

        var result = new CreateProductCommandValidator().Validate(command);

        Assert.Contains(result.Errors, e => e.ErrorCode == "Product.Name.TooLong");
    }

    [Fact]
    public void Update_EmptyId_Fails()
    {
        var command = new UpdateProductCommand(
            Guid.Empty, "Boots", "Warm", "/img.png", 10m, Guid.NewGuid(), Guid.NewGuid());

        var result = new UpdateProductCommandValidator().Validate(command);

        Assert.Contains(result.Errors, e => e.ErrorCode == "Product.Id.Required");
    }
}
