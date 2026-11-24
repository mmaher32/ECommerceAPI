using Asp.Versioning;
using Asp.Versioning.Builder;
using ECommerce.API.Extensions;
using ECommerce.API.Filters;
using ECommerce.API.Models;
using ECommerce.API.Security;
using ECommerce.UseCases.Products.Commands.CreateProduct;
using ECommerce.UseCases.Products.Commands.DeleteProduct;
using ECommerce.UseCases.Products.Commands.UpdateProduct;
using ECommerce.UseCases.Products.Dtos;
using ECommerce.UseCases.Products.Queries.GetByIdProduct;
using ECommerce.UseCases.Products.Queries.GetPagedProduct;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;

namespace ECommerce.API.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(
        this IEndpointRouteBuilder endpoints,
        ApiVersionSet apiVersionSet
        )
    {
        var group = endpoints
            .MapGroup("/api/v{version:apiVersion}/products")
            .WithTags("Products")
            .WithApiVersionSet(apiVersionSet)
            .HasApiVersion(new ApiVersion(1, 0))
            .AddEndpointFilter<AuditEndpointFilter>();

        //Paged
        //GET /api/v1/products/paged

        group.MapGet("/paged", async (
            [AsParameters] GetPagedProductQuery query,
            ISender sender,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);

            return result.FromPagedResult(httpContext,
                query.PageNumber,
                query.PageSize,
                "Products retrieved successfully");
        })
            .WithSummary("Paginated Results.")
            .WithDescription("Returns a paginated list of products with filtering and sorting options")
            .CacheOutput("Products")
            .Produces<ApiResponse<IReadOnlyList<GetAllProductsResponse>>>(StatusCodes.Status200OK);

        //GetById
        group.MapGet("/{id:guid}", async (
                Guid id,
                ISender sender,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetByIdProductQuery(id), cancellationToken);

            return result.FromResult(httpContext, "Product id retrieved successfully");
        })
            .WithName("GetProductById")
            .WithSummary("Gets product by ID")
            .WithDescription("Returns product information")
            .Produces<ApiResponse<GetByIdProductResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Catalog writes. Every one of them evicts the cached product pages, otherwise
        // /paged keeps serving the old catalog until the 1-minute expiry.
        var admin = group.MapGroup("")
            .RequireAuthorization(AuthorizationPolicies.CatalogAdmin);

        //POST /api/v1/products
        admin.MapPost("/", async (
            UpsertProductRequest request,
            ISender sender,
            IOutputCacheStore cache,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateProductCommand(
                    request.Name,
                    request.Description,
                    request.PictureUrl,
                    request.Price,
                    request.ProductBrandId,
                    request.ProductTypeId),
                cancellationToken);

            if (result.IsSuccess)
                await cache.EvictByTagAsync(OutputCacheTags.Products, cancellationToken);

            return result.FromResult(httpContext, "Product created successfully");
        })
            .WithSummary("Creates a product")
            .WithDescription("Admin or SuperAdmin only.")
            .Produces<ApiResponse<GetByIdProductResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        //PUT /api/v1/products/{id}
        admin.MapPut("/{id:guid}", async (
            Guid id,
            UpsertProductRequest request,
            ISender sender,
            IOutputCacheStore cache,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateProductCommand(
                    id,
                    request.Name,
                    request.Description,
                    request.PictureUrl,
                    request.Price,
                    request.ProductBrandId,
                    request.ProductTypeId),
                cancellationToken);

            if (result.IsSuccess)
                await cache.EvictByTagAsync(OutputCacheTags.Products, cancellationToken);

            return result.FromResult(httpContext, "Product updated successfully");
        })
            .WithSummary("Replaces a product's details")
            .WithDescription("Admin or SuperAdmin only. Existing orders keep the price they were placed at.")
            .Produces<ApiResponse<GetByIdProductResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        //DELETE /api/v1/products/{id}
        admin.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            IOutputCacheStore cache,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id), cancellationToken);

            if (result.IsSuccess)
                await cache.EvictByTagAsync(OutputCacheTags.Products, cancellationToken);

            return result.FromResult(httpContext, "Product deleted successfully");
        })
            .WithSummary("Soft-deletes a product")
            .WithDescription("Admin or SuperAdmin only. The row is kept so past orders still resolve.")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

}
