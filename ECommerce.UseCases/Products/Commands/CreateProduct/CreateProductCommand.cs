using ECommerce.Domain.Shared;
using ECommerce.UseCases.Products.Dtos;
using MediatR;

namespace ECommerce.UseCases.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string PictureUrl,
    decimal Price,
    Guid ProductBrandId,
    Guid ProductTypeId) : IRequest<Result<GetByIdProductResponse>>;
