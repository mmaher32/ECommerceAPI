namespace ECommerce.UseCases.Products.Dtos;

/// <summary>
/// Body for both create and update. The id of an update comes from the route,
/// never from the body, so a client cannot aim a body at a different product.
/// </summary>
public record UpsertProductRequest(
    string Name,
    string Description,
    string PictureUrl,
    decimal Price,
    Guid ProductBrandId,
    Guid ProductTypeId);
