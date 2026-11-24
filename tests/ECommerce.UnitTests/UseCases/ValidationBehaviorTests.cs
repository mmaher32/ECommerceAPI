using ECommerce.Domain.Shared;
using ECommerce.UseCases.Behaviors;
using ECommerce.UseCases.Products.Commands.DeleteProduct;
using ECommerce.UseCases.Products.Commands.UpdateProduct;
using ECommerce.UseCases.Products.Commands.Validators;
using ECommerce.UseCases.Products.Dtos;
using FluentValidation;

namespace ECommerce.UnitTests.UseCases;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task InvalidRequest_ReturnsGenericResultFailure_WithoutCallingHandler()
    {
        var behavior = new ValidationBehavior<UpdateProductCommand, Result<GetByIdProductResponse>>(
            [new UpdateProductCommandValidator()]);

        var handlerCalled = false;

        var result = await behavior.Handle(
            new UpdateProductCommand(Guid.Empty, "", "d", "/p", 1m, Guid.NewGuid(), Guid.NewGuid()),
            _ =>
            {
                handlerCalled = true;
                return Task.FromResult(Result<GetByIdProductResponse>.Success(null!));
            },
            CancellationToken.None);

        Assert.False(handlerCalled);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains("Product.Id.Required", result.Error.Code);
        Assert.Contains("Product.Name.Required", result.Error.Code);
    }

    [Fact]
    public async Task NoValidators_CallsHandler()
    {
        var behavior = new ValidationBehavior<DeleteProductCommand, Result>(
            Array.Empty<IValidator<DeleteProductCommand>>());

        var result = await behavior.Handle(
            new DeleteProductCommand(Guid.NewGuid()),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
