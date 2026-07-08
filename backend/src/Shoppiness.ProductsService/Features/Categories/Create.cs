using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Categories;

public static class Create
{
    public sealed record Request(string Name, string? Description, Guid? ParentCategoryId);

    public sealed record Response(
        Guid Id,
        string Name,
        string? Description,
        Guid? ParentCategoryId,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200);
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

        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await db.Categories
                .AnyAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
                return TypedResults.NotFound($"Parent category '{request.ParentCategoryId}' not found.");
        }

        var now = DateTime.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        var response = new Response(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.CreatedAt,
            category.UpdatedAt);

        return TypedResults.Created($"/categories/{category.Id}", response);
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/categories", HandleAsync)
            .WithName("CreateCategory")
            .WithTags("Categories")
            .WithSummary("Create a new category")
            .Produces<Response>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
