using Ardalis.Specification;
using ECommerce.Domain.Entities;

namespace ECommerce.UseCases.Products.Specifications;

/// <summary>
/// Matches another live product with the same name, ignoring case and surrounding
/// whitespace. <paramref name="excludeId"/> lets an update keep its own name.
/// </summary>
public sealed class ProductNameTakenSpecification : Specification<Product>
{
    public ProductNameTakenSpecification(string name, Guid? excludeId = null)
    {
        var normalized = name.Trim().ToLower();

        Query.Where(p => p.Name.ToLower() == normalized);

        if (excludeId.HasValue)
        {
            var id = excludeId.Value;
            Query.Where(p => p.Id != id);
        }
    }
}
