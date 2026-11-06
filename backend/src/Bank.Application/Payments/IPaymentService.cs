namespace Bank.Application.Payments;

public interface IPaymentService
{
    Task<TransactionDto> ProcessDepositAsync(
        DepositRequest request,
        string userId,
        Guid? idempotencyKeyId = null,
        CancellationToken ct = default);

    Task<TransactionDto> ProcessWithdrawalAsync(
        WithdrawalRequest request,
        string userId,
        Guid? idempotencyKeyId = null,
        CancellationToken ct = default);

    Task<TransactionDto> ProcessTransferAsync(
        TransferRequest request,
        string userId,
        Guid? idempotencyKeyId = null,
        CancellationToken ct = default);

    Task<TransactionDto> ProcessReversalAsync(
        Guid transactionId,
        ReversalRequest request,
        string adminUserId,
        Guid? idempotencyKeyId = null,
        CancellationToken ct = default);

    Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId, CancellationToken ct = default);
    Task<TrialBalanceDto> GetTrialBalanceAsync(CancellationToken ct = default);
}
