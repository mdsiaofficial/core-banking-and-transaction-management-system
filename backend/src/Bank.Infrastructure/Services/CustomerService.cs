using Bank.Application.Customers;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bank.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly BankDbContext _db;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(BankDbContext db, ILogger<CustomerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        return customer is null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto?> GetCustomerByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        return customer is null ? null : MapToDto(customer);
    }

    public async Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(
        string? searchTerm = null,
        string? status = null,
        int limit = 50,
        CancellationToken ct = default)
    {
        var query = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.FullName, pattern) ||
                EF.Functions.ILike(c.CustomerNo, pattern) ||
                EF.Functions.ILike(c.Phone, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CustomerStatus>(status, true, out var st))
        {
            query = query.Where(c => c.Status == st);
        }

        int clampedLimit = Math.Clamp(limit, 1, 100);
        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(clampedLimit)
            .ToListAsync(ct);

        return customers.Select(MapToDto).ToList();
    }

    public async Task UpdateProfileAsync(Guid customerId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { customerId }, ct);
        if (customer is null)
            throw new DomainException($"Customer {customerId} not found.", "CUSTOMER_NOT_FOUND");

        customer.FullName = request.FullName;
        customer.Phone = request.Phone;
        customer.AddressJson = request.AddressJson;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Profile updated for customer {CustomerId}", customerId);
    }

    public async Task SuspendCustomerAsync(Guid customerId, string reason, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { customerId }, ct);
        if (customer is null)
            throw new DomainException($"Customer {customerId} not found.", "CUSTOMER_NOT_FOUND");

        customer.Suspend();
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Customer {CustomerId} suspended. Reason: {Reason}", customerId, reason);
    }

    public async Task ReactivateCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { customerId }, ct);
        if (customer is null)
            throw new DomainException($"Customer {customerId} not found.", "CUSTOMER_NOT_FOUND");

        customer.Activate();
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Customer {CustomerId} reactivated.", customerId);
    }

    private static CustomerDto MapToDto(Customer c) => new(
        c.Id,
        c.UserId,
        c.CustomerNo,
        c.FullName,
        c.DateOfBirth,
        c.Phone,
        c.AddressJson,
        c.Status.ToString(),
        c.KycStatus.ToString(),
        c.CreatedAt);
}
