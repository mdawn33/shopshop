using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Categories;

public static class Update
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

        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
            return TypedResults.NotFound();

        category.Name = request.Name;
        category.Description = request.Description;
        category.ParentCategoryId = request.ParentCategoryId;

        await db.SaveChangesAsync(cancellationToken);

        var response = new Response(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.CreatedAt,
            category.UpdatedAt);

        return TypedResults.Ok(response);
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapPut("/categories/{id:guid}", HandleAsync)
            .WithName("UpdateCategory")
            .WithTags("Categories")
            .WithSummary("Update a category's name, description, or parent")
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
