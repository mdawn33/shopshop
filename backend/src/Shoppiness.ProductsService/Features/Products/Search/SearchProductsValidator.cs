using FluentValidation;

namespace Shoppiness.ProductService.Features.Products.Search;

public sealed class SearchProductsValidator : AbstractValidator<SearchProductsRequest>
{
    public SearchProductsValidator()
    {
        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.MinPrice.HasValue)
            .WithMessage("MinPrice must be greater than or equal to 0.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("MaxPrice must be greater than or equal to 0.");

        RuleFor(x => x.MinPrice)
            .Must((request, minPrice) => minPrice is null || request.MaxPrice is null || minPrice <= request.MaxPrice)
            .WithMessage("MinPrice must be less than or equal to MaxPrice.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(SearchProductsRequest.DefaultPage)
            .WithMessage($"Page must be greater than or equal to {SearchProductsRequest.DefaultPage}.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, SearchProductsRequest.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {SearchProductsRequest.MaxPageSize}.");

        RuleFor(x => x.SortBy)
            .IsInEnum()
            .WithMessage("SortBy must be one of: price, name, newest.");

        RuleFor(x => x.SortDirection)
            .IsInEnum()
            .WithMessage("SortDirection must be one of: asc, desc.");
    }
}
