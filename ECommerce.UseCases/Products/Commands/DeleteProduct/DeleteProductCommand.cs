using ECommerce.Domain.Shared;
using MediatR;

namespace ECommerce.UseCases.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;
