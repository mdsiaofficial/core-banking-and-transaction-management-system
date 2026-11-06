namespace Bank.Application.Customers;

public record CustomerDto(
    Guid Id,
    Guid UserId,
    string CustomerNo,
    string FullName,
    DateOnly DateOfBirth,
    string Phone,
    string AddressJson,
    string Status,
    string KycStatus,
    DateTimeOffset CreatedAt);

public record UpdateProfileRequest(
    string FullName,
    string Phone,
    string AddressJson);

public interface ICustomerService
{
    Task<CustomerDto?> GetCustomerByIdAsync(Guid customerId, CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string? searchTerm = null, string? status = null, int limit = 50, CancellationToken ct = default);
    Task UpdateProfileAsync(Guid customerId, UpdateProfileRequest request, CancellationToken ct = default);
    Task SuspendCustomerAsync(Guid customerId, string reason, CancellationToken ct = default);
    Task ReactivateCustomerAsync(Guid customerId, CancellationToken ct = default);
}
