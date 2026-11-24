using ECommerce.Domain.Entities;
using ECommerce.UseCases.Products.Commands.UpdateProduct;
using FluentValidation;

namespace ECommerce.UseCases.Products.Commands.Validators;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty()
            .WithErrorCode("Product.Id.Required")
            .WithMessage("A product id is required.");

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
