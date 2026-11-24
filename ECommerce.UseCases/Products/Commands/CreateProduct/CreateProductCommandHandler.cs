using ECommerce.Domain.Entities;
using ECommerce.Domain.Errors;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.Shared;
using ECommerce.UseCases.Products.Dtos;
using MediatR;

namespace ECommerce.UseCases.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IUnitOfWork unitOfWork,
    IProductQueryService productQueryService) :
    IRequestHandler<CreateProductCommand, Result<GetByIdProductResponse>>
{
    public async Task<Result<GetByIdProductResponse>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var checks = await ProductCatalogChecks.EnsureCanUseAsync(
            unitOfWork,
            request.Name,
            request.ProductBrandId,
            request.ProductTypeId,
            excludeProductId: null,
            cancellationToken);

        if (checks.IsFailure)
            return Result<GetByIdProductResponse>.Failure(checks.Error!);

        var productResult = Product.Create(
            request.Name,
            request.Description,
            request.PictureUrl,
            request.Price,
            request.ProductBrandId,
            request.ProductTypeId);

        if (productResult.IsFailure)
            return Result<GetByIdProductResponse>.Failure(productResult.Error!);

        var product = productResult.Value;

        unitOfWork.Repository<Product>().Add(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-read through the query service so the response has the same shape
        // (brand and type names, not ids) as GET /products/{id}.
        var response = await productQueryService.GetByIdProductAsync(product.Id, cancellationToken);

        return response is null
            ? Result<GetByIdProductResponse>.Failure(ProductErrors.NotFound)
            : Result<GetByIdProductResponse>.Success(response);
    }
}
