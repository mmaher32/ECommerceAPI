using Ardalis.Specification;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Errors;
using ECommerce.Domain.Repositories;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.Commands.CreateProduct;
using ECommerce.UseCases.Products.Commands.DeleteProduct;
using ECommerce.UseCases.Products.Commands.UpdateProduct;
using ECommerce.UseCases.Products.Dtos;
using NSubstitute;

namespace ECommerce.UnitTests.UseCases;

public class ProductCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Product> _products = Substitute.For<IRepository<Product>>();
    private readonly IRepository<ProductBrand> _brands = Substitute.For<IRepository<ProductBrand>>();
    private readonly IRepository<ProductType> _types = Substitute.For<IRepository<ProductType>>();
    private readonly IProductQueryService _queries = Substitute.For<IProductQueryService>();

    private readonly ProductBrand _brand = ProductBrand.Create(Guid.NewGuid(), "Acme").Value;
    private readonly ProductType _type = ProductType.Create(Guid.NewGuid(), "Boots").Value;

    public ProductCommandHandlerTests()
    {
        _unitOfWork.Repository<Product>().Returns(_products);
        _unitOfWork.Repository<ProductBrand>().Returns(_brands);
        _unitOfWork.Repository<ProductType>().Returns(_types);

        _brands.GetByIdAsync(_brand.Id, Arg.Any<CancellationToken>()).Returns(_brand);
        _types.GetByIdAsync(_type.Id, Arg.Any<CancellationToken>()).Returns(_type);

        _queries.GetByIdProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => new GetByIdProductResponse(
                call.Arg<Guid>(), "Boots", "Warm", 10m, "/img.png", "Boots", "Acme"));
    }

    private CreateProductCommand CreateCommand(string name = "Boots") =>
        new(name, "Warm", "/img.png", 10m, _brand.Id, _type.Id);

    private Product ExistingProduct() =>
        Product.Create("Old", "Old", "/old.png", 5m, _brand.Id, _type.Id).Value;

    [Fact]
    public async Task Create_HappyPath_AddsAndSaves()
    {
        var handler = new CreateProductCommandHandler(_unitOfWork, _queries);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _products.Received(1).Add(Arg.Is<Product>(p => p.Name == "Boots" && p.Price == 10m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_UnknownBrand_ReturnsNotFound_AndDoesNotSave()
    {
        var handler = new CreateProductCommandHandler(_unitOfWork, _queries);

        var result = await handler.Handle(
            CreateCommand() with { ProductBrandId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Equal(ProductBrandErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_UnknownType_ReturnsNotFound()
    {
        var handler = new CreateProductCommandHandler(_unitOfWork, _queries);

        var result = await handler.Handle(
            CreateCommand() with { ProductTypeId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Equal(ProductTypeErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        _products.AnyAsync(Arg.Any<ISpecification<Product>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CreateProductCommandHandler(_unitOfWork, _queries);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ProductErrors.NameAlreadyExists, result.Error);
        _products.DidNotReceive().Add(Arg.Any<Product>());
    }

    [Fact]
    public async Task Update_HappyPath_ChangesTrackedEntity()
    {
        var product = ExistingProduct();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new UpdateProductCommandHandler(_unitOfWork, _queries);

        var result = await handler.Handle(
            new UpdateProductCommand(product.Id, "New", "New desc", "/new.png", 20m, _brand.Id, _type.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", product.Name);
        Assert.Equal(20m, product.Price);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_MissingProduct_ReturnsNotFound()
    {
        var handler = new UpdateProductCommandHandler(_unitOfWork, _queries);

        var result = await handler.Handle(
            new UpdateProductCommand(Guid.NewGuid(), "New", "d", "/p", 1m, _brand.Id, _type.Id),
            CancellationToken.None);

        Assert.Equal(ProductErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Update_DeletedProduct_ReturnsNotFound()
    {
        var product = ExistingProduct();
        product.MarkAsDeleted();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new UpdateProductCommandHandler(_unitOfWork, _queries);

        var result = await handler.Handle(
            new UpdateProductCommand(product.Id, "New", "d", "/p", 1m, _brand.Id, _type.Id),
            CancellationToken.None);

        Assert.Equal(ProductErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ExistingProduct_SoftDeletesAndSaves()
    {
        var product = ExistingProduct();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new DeleteProductCommandHandler(_unitOfWork);

        var result = await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _products.Received(1).Delete(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_MissingProduct_ReturnsNotFound()
    {
        var handler = new DeleteProductCommandHandler(_unitOfWork);

        var result = await handler.Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ProductErrors.NotFound, result.Error);
    }
}
