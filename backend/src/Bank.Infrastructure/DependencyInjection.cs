using Bank.Domain.Services;
using Bank.Infrastructure.Auth;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=bankdb;Username=postgres;Password=postgres";

        services.AddDbContext<BankDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(BankDbContext).Assembly.FullName);
            });
            options.UseSnakeCaseNamingConvention();
        });

        // Register domain services
        services.AddScoped<IPostingService, PostingService>();
        services.AddScoped<ITransferPolicy, TransferPolicy>();
        services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();

        // Register application services
        services.AddScoped<Bank.Application.Idempotency.IIdempotencyService, Bank.Infrastructure.Services.IdempotencyService>();
        services.AddScoped<Bank.Application.Accounts.IAccountService, Bank.Infrastructure.Services.AccountService>();
        services.AddScoped<Bank.Application.Payments.IPaymentService, Bank.Infrastructure.Services.PaymentService>();
        services.AddScoped<Bank.Application.Customers.ICustomerService, Bank.Infrastructure.Services.CustomerService>();
        services.AddScoped<Bank.Application.Reconciliation.IReconciliationService, Bank.Infrastructure.Services.ReconciliationService>();

        // Register Database Migrator
        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        // Register Security & Identity
        services.AddBankSecurity(configuration);

        return services;
    }
}
