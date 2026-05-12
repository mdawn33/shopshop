namespace SharedContracts.Events;

public record UpdateStockEvent(Guid ProductId, int CountChange);