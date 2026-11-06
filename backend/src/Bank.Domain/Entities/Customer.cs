using Bank.Domain.Enums;
using Bank.Domain.Exceptions;

namespace Bank.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string CustomerNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string AddressJson { get; set; } = "{}";
    public CustomerStatus Status { get; set; } = CustomerStatus.PendingVerification;
    public KycStatus KycStatus { get; set; } = KycStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Account> Accounts { get; set; } = new List<Account>();

    public void Activate()
    {
        if (Status == CustomerStatus.Closed)
            throw new DomainException("Cannot activate a closed customer account.", "CUSTOMER_CLOSED");

        Status = CustomerStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend()
    {
        if (Status == CustomerStatus.Closed)
            throw new DomainException("Cannot suspend a closed customer account.", "CUSTOMER_CLOSED");

        Status = CustomerStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ApproveKyc()
    {
        KycStatus = KycStatus.Approved;
        if (Status == CustomerStatus.PendingVerification)
        {
            Status = CustomerStatus.Active;
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RejectKyc()
    {
        KycStatus = KycStatus.Rejected;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        Status = CustomerStatus.Closed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
