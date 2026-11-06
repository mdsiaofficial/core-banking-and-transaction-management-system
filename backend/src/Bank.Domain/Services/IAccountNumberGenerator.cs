namespace Bank.Domain.Services;

public interface IAccountNumberGenerator
{
    string Generate(long sequenceNumber, string branchCode = "101");
}
