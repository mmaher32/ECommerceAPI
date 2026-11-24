using ECommerce.Domain.Entities;
using ECommerce.Domain.Errors;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.Shared;
using MediatR;

namespace ECommerce.UseCases.Products.Commands.DeleteProduct;

/// <summary>
/// Soft delete. Past orders keep their own copy of the name and price, and a basket
/// that still holds the product fails at checkout because the pricing lookup no
/// longer finds it, so nothing downstream breaks silently.
/// </summary>
public sealed class DeleteProductCommandHandler(IUnitOfWork unitOfWork) :
    IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Repository<Product>();

        var product = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null || product.IsDeleted)
            return Result.Failure(ProductErrors.NotFound);

        repository.Delete(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
