using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Products;

public static class Create
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

    
    public static async Task<Results<Created<Response>, BadRequest<ValidationProblemDetails>, NotFound<string>>> HandleAsync(
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

        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
            return TypedResults.NotFound($"Category '{request.CategoryId}' not found.");

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        var response = new Response(
            product.Id,
            product.Name,
            product.Description,
            product.BasePrice,
            product.CategoryId,
            category.Name,
            product.CreatedAt,
            product.UpdatedAt);

        return TypedResults.Created($"/products/{product.Id}", response);
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/products", HandleAsync)
            .WithName("CreateProduct")
            .WithTags("Products")
            .WithSummary("Create a new product")
            .Produces<Response>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
