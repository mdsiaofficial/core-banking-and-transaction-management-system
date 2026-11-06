using Bank.Infrastructure;
using Bank.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bank.Migrator;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=========================================================");
        Console.WriteLine("  Bank.Migrator: PostgreSQL Database Migration & Seed");
        Console.WriteLine("=========================================================");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConfiguration(configuration.GetSection("Logging"));
            logging.AddConsole();
        });

        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Bank.Migrator");

        try
        {
            logger.LogInformation("Starting database migration process...");
            await migrator.MigrateAsync();
            logger.LogInformation("Migrations applied successfully.");

            bool seedDemo = configuration.GetValue("SEED_DEMO_DATA", true);
            if (seedDemo)
            {
                logger.LogInformation("Seeding initial demo chart of accounts, users, and accounts...");
                await migrator.SeedDemoDataAsync();
                logger.LogInformation("Demo data seeding completed.");
            }

            Console.WriteLine("[Bank.Migrator] All migrations and seeding completed successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error occurred during database migration/seeding: {Message}", ex.Message);
            Console.Error.WriteLine($"[Bank.Migrator] Fatal error: {ex}");
            return 1;
        }
    }
}
