using FluentValidation;

namespace Shoppiness.ProductService.Features.Products.Purchase;

public sealed class PurchaseProductValidator : AbstractValidator<PurchaseProductRequest>
{
    public PurchaseProductValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.");
    }
}
