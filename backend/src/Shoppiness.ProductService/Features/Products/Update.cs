using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Products;

public static class Update
{
    public sealed record Request(string Name, string? Description, decimal BasePrice, Guid CategoryId);

    public sealed record Response(
        Guid Id,
        string Name,
        string? Description,
        decimal BasePrice,
        Guid CategoryId,
        string CategoryName,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(300);

            RuleFor(x => x.BasePrice)
                .GreaterThan(0).WithMessage("BasePrice must be greater than 0.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("CategoryId is required.");
        }
    }

    public static async Task<Results<Ok<Response>, BadRequest<ValidationProblemDetails>, NotFound>> HandleAsync(
        Guid id,
        Request request,
        IValidator<Request> validator,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return TypedResults.BadRequest(new ValidationProblemDetails(errors));
        }

        var product = await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return TypedResults.NotFound();

        product.Name = request.Name;
        product.Description = request.Description;
        product.BasePrice = request.BasePrice;
        product.CategoryId = request.CategoryId;

        await db.SaveChangesAsync(cancellationToken);

        var response = new Response(
            product.Id,
            product.Name,
            product.Description,
            product.BasePrice,
            product.CategoryId,
            product.Category!.Name,
            product.CreatedAt,
            product.UpdatedAt);

        return TypedResults.Ok(response);
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapPut("/products/{id:guid}", HandleAsync)
            .WithName("UpdateProduct")
            .WithTags("Products")
            .WithSummary("Update a product's name, description, base price, or category")
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
