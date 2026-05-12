using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shoppiness.StockService.Messaging;
using StockService.Domain.Entities;
using StockService.Domain.Enums;
using StockService.Infrastructure.Persistence;

namespace Shoppiness.StockService.Features.Stocks;

public static class RemoveStock
{
    public sealed record Request(int Quantity, MovementType MovementType, string? ReferenceType, Guid? ReferenceId);

    public sealed record Response(Guid ProductId, int NewQuantity);

    public sealed class Validator : AbstractValidator<Request>
    {
        private static readonly MovementType[] ValidTypes = [MovementType.SalesDeduction, MovementType.ManualAdjustment];

        public Validator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

            RuleFor(x => x.MovementType)
                .Must(t => ValidTypes.Contains(t))
                .WithMessage($"MovementType must be one of: {string.Join(", ", ValidTypes.Select(t => t.ToString()))}.");
        }
    }

    public static async Task<Results<Ok<Response>, BadRequest<ValidationProblemDetails>, NotFound, Conflict<string>>> HandleAsync(
        Guid productId,
        Request request,
        IValidator<Request> validator,
        StockDbContext db,
        IStockEventPublisher eventPublisher,
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

        var stock = await db.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);

        if (stock is null)
            return TypedResults.NotFound();

        if (stock.Quantity < request.Quantity)
            return TypedResults.Conflict($"Insufficient stock. Current quantity is {stock.Quantity}, requested removal is {request.Quantity}.");

        stock.Quantity -= request.Quantity;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            StockId = stock.Id,
            MovementType = request.MovementType,
            Quantity = request.Quantity,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            CreatedAt = DateTime.UtcNow
        };

        db.StockMovements.Add(movement);
        await db.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishStockUpdatedAsync(stock.ProductId, stock.Quantity);

        return TypedResults.Ok(new Response(stock.ProductId, stock.Quantity));
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapPatch("/stock/{productId:guid}/remove", HandleAsync)
            .WithName("RemoveStock")
            .WithTags("Stock")
            .WithSummary("Remove stock quantity for a product (SalesDeduction or ManualAdjustment)")
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<string>(StatusCodes.Status409Conflict);
    }
}
