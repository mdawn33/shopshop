using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Shoppiness.ProductService.Features.Products.Purchase;

public static class PurchaseProductEndpoint
{
    
    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/products/{id:guid}/purchase", HandleAsync)
            .WithName("PurchaseProduct")
            .WithTags("Products")
            .WithSummary("Purchase a product — resolves active price, checks stock, notifies payment service")
            .Produces<PurchaseProductResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<string>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        PurchaseProductRequest request,
        IValidator<PurchaseProductRequest> validator,
        IPurchaseProductHandler handler,
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

        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.StatusCode switch
        {
            StatusCodes.Status404NotFound => TypedResults.NotFound(result.Error),
            StatusCodes.Status409Conflict => TypedResults.Conflict(result.Error),
            StatusCodes.Status200OK => TypedResults.Ok(result.Response),
            _ => TypedResults.StatusCode(result.StatusCode)
        };
    }
    
}
