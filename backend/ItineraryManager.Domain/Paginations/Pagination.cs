namespace ItineraryManager.Domain.Paginations;

public record PaginationRequest(int Offset = 0, int Limit = 10);

public record PaginationResponse(int Offset, int Limit, int Total);

public record Paginated<T>(T[] Data, PaginationResponse Pagination);