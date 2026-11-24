using ECommerce.Domain.Entities;
using ECommerce.UseCases.Products.Commands.CreateProduct;
using FluentValidation;

namespace ECommerce.UseCases.Products.Commands.Validators;

/// <summary>
/// Mirrors the guards in <see cref="Product"/> so a bad request gets every problem
/// back at once, instead of only the first one the entity hits.
/// </summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithErrorCode("Product.Name.Required")
            .WithMessage("Product name is required.")
            .MaximumLength(Product.MaxNameLength)
            .WithErrorCode("Product.Name.TooLong")
            .WithMessage($"Product name cannot exceed {Product.MaxNameLength} characters.");

        RuleFor(c => c.Description)
            .NotEmpty()
            .WithErrorCode("Product.Description.Required")
            .WithMessage("Product description is required.")
            .MaximumLength(Product.MaxDescriptionLength)
            .WithErrorCode("Product.Description.TooLong")
            .WithMessage($"Product description cannot exceed {Product.MaxDescriptionLength} characters.");

        RuleFor(c => c.PictureUrl)
            .NotEmpty()
            .WithErrorCode("Product.PictureUrl.Required")
            .WithMessage("Picture URL is required.")
            .MaximumLength(Product.MaxPictureUrlLength)
            .WithErrorCode("Product.PictureUrl.TooLong")
            .WithMessage($"Picture URL cannot exceed {Product.MaxPictureUrlLength} characters.");

        RuleFor(c => c.Price)
            .GreaterThan(0)
            .WithErrorCode("Product.Price.Invalid")
            .WithMessage("Price must be greater than zero.");

        RuleFor(c => c.ProductBrandId)
            .NotEmpty()
            .WithErrorCode("Product.Brand.Required")
            .WithMessage("A product brand is required.");

        RuleFor(c => c.ProductTypeId)
            .NotEmpty()
            .WithErrorCode("Product.Type.Required")
            .WithMessage("A product type is required.");
    }
}
