using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockService.Infrastructure.Persistence;
using StockService.Domain.Entities;

namespace Shoppiness.StockService.Features.Stocks;

public static class InitializeStock
{
    public sealed record Request(Guid ProductId);

    public sealed record Response(Guid Id, Guid ProductId, int Quantity);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("ProductId is required.");
        }
    }

    public static async Task<Results<Created<Response>, BadRequest<ValidationProblemDetails>, Conflict<string>>> HandleAsync(
        Request request,
        IValidator<Request> validator,
        StockDbContext db,
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

        // Check for any existing record (active or soft-deleted) — design.md Risk: re-initialization
        var existingAny = await db.Stocks
            .IgnoreQueryFilters()
            .AnyAsync(s => s.ProductId == request.ProductId, cancellationToken);

        if (existingAny)
            return TypedResults.Conflict($"A Stock record for ProductId '{request.ProductId}' already exists.");

        var now = DateTime.UtcNow;


        var stock = new Stock
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Quantity = 0,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };

        db.Stocks.Add(stock);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/stock/{stock.ProductId}", new Response(stock.Id, stock.ProductId, stock.Quantity));
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/stock", HandleAsync)
            .WithName("InitializeStock")
            .WithTags("Stock")
            .WithSummary("Initialize a stock record for a product")
            .Produces<Response>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status409Conflict);
    }
}
