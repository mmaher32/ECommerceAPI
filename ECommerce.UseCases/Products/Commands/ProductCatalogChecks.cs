using ECommerce.Domain.Entities;
using ECommerce.Domain.Errors;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.Shared;
using ECommerce.UseCases.Products.Specifications;

namespace ECommerce.UseCases.Products.Commands;

/// <summary>
/// The checks create and update share: the brand and type must exist (the foreign key
/// would catch it too, but as a 500 instead of a 404) and the name must be free.
/// </summary>
internal static class ProductCatalogChecks
{
    public static async Task<Result> EnsureCanUseAsync(
        IUnitOfWork unitOfWork,
        string name,
        Guid brandId,
        Guid typeId,
        Guid? excludeProductId,
        CancellationToken cancellationToken)
    {
        var brand = await unitOfWork.Repository<ProductBrand>()
            .GetByIdAsync(brandId, cancellationToken);

        if (brand is null || brand.IsDeleted)
            return Result.Failure(ProductBrandErrors.NotFound);

        var type = await unitOfWork.Repository<ProductType>()
            .GetByIdAsync(typeId, cancellationToken);

        if (type is null || type.IsDeleted)
            return Result.Failure(ProductTypeErrors.NotFound);

        var nameTaken = await unitOfWork.Repository<Product>()
            .AnyAsync(new ProductNameTakenSpecification(name, excludeProductId), cancellationToken);

        if (nameTaken)
            return Result.Failure(ProductErrors.NameAlreadyExists);

        return Result.Success();
    }
}
