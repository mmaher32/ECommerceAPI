using ECommerce.Domain.Shared;
using ECommerce.UseCases.Products.Dtos;
using MediatR;

namespace ECommerce.UseCases.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    string PictureUrl,
    decimal Price,
    Guid ProductBrandId,
    Guid ProductTypeId) : IRequest<Result<GetByIdProductResponse>>;
