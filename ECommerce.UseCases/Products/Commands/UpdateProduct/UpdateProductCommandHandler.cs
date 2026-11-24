using ECommerce.Domain.Entities;
using ECommerce.Domain.Errors;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.Shared;
using ECommerce.UseCases.Products.Dtos;
using MediatR;

namespace ECommerce.UseCases.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IUnitOfWork unitOfWork,
    IProductQueryService productQueryService) :
    IRequestHandler<UpdateProductCommand, Result<GetByIdProductResponse>>
{
    public async Task<Result<GetByIdProductResponse>> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Repository<Product>()
            .GetByIdAsync(request.Id, cancellationToken);

        if (product is null || product.IsDeleted)
            return Result<GetByIdProductResponse>.Failure(ProductErrors.NotFound);

        var checks = await ProductCatalogChecks.EnsureCanUseAsync(
            unitOfWork,
            request.Name,
            request.ProductBrandId,
            request.ProductTypeId,
            excludeProductId: product.Id,
            cancellationToken);

        if (checks.IsFailure)
            return Result<GetByIdProductResponse>.Failure(checks.Error!);

        var updateResult = product.Update(
            request.Name,
            request.Description,
            request.PictureUrl,
            request.Price,
            request.ProductBrandId,
            request.ProductTypeId);

        if (updateResult.IsFailure)
            return Result<GetByIdProductResponse>.Failure(updateResult.Error!);

        // GetByIdAsync uses Find, so the entity is already tracked and the change
        // tracker picks up the edits; no explicit Update call is needed.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await productQueryService.GetByIdProductAsync(product.Id, cancellationToken);

        return response is null
            ? Result<GetByIdProductResponse>.Failure(ProductErrors.NotFound)
            : Result<GetByIdProductResponse>.Success(response);
    }
}
