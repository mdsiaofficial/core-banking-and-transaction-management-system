using Bank.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bank.Api.IntegrationTests.Fixtures;

public class PostgreSqlTestFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; }

    public string ConnectionString => Container.GetConnectionString();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public PostgreSqlTestFixture()
    {
        Container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("bank_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();

        // Apply raw SQL migration scripts directly to the real PostgreSQL container
        await ApplyScriptsAsync();

        // Set environment variable so WebApplication.CreateBuilder picks it up immediately
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "SuperSecretIntegrationTestingKeyMustBeAtLeast32BytesLong!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "https://bank.local");
        Environment.SetEnvironmentVariable("Jwt__Audience", "https://bank.local");

        // Initialize WebApplicationFactory with test database connection
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.UseSetting("Jwt:SecretKey", "SuperSecretIntegrationTestingKeyMustBeAtLeast32BytesLong!");
            builder.UseSetting("Jwt:Issuer", "https://bank.local");
            builder.UseSetting("Jwt:Audience", "https://bank.local");
            builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", "15");
            builder.UseSetting("Jwt:RefreshTokenLifetimeDays", "7");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                    ["Jwt:SecretKey"] = "SuperSecretIntegrationTestingKeyMustBeAtLeast32BytesLong!",
                    ["Jwt:Issuer"] = "https://bank.local",
                    ["Jwt:Audience"] = "https://bank.local",
                    ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                    ["Jwt:RefreshTokenLifetimeDays"] = "7"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<BankDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(BankDbContext)).ToList();

                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                services.AddDbContext<BankDbContext>(options =>
                {
                    options.UseNpgsql(ConnectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(BankDbContext).Assembly.FullName);
                    });
                    options.UseSnakeCaseNamingConvention();
                });
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            });
        });

        // Seed demo data (roles, users, chart of accounts) using DatabaseMigrator
        using var scope = Factory.Services.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
        await migrator.SeedDemoDataAsync();
    }

    private async Task ApplyScriptsAsync()
    {
        string baseDir = AppContext.BaseDirectory;
        string? scriptsDir = null;

        // Traverse up to find backend/src/Bank.Infrastructure/Data/Scripts
        string dir = baseDir;
        for (int i = 0; i < 7; i++)
        {
            string candidate = Path.Combine(dir, "src", "Bank.Infrastructure", "Data", "Scripts");
            if (Directory.Exists(candidate))
            {
                scriptsDir = candidate;
                break;
            }
            candidate = Path.Combine(dir, "backend", "src", "Bank.Infrastructure", "Data", "Scripts");
            if (Directory.Exists(candidate))
            {
                scriptsDir = candidate;
                break;
            }
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }

        if (scriptsDir is null)
        {
            throw new InvalidOperationException("Could not locate Data/Scripts directory for integration tests.");
        }

        var scripts = new[]
        {
            Path.Combine(scriptsDir, "001_schema.sql"),
            Path.Combine(scriptsDir, "002_triggers.sql"),
            Path.Combine(scriptsDir, "003_seed_chart_of_accounts.sql")
        };

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        foreach (var scriptPath in scripts)
        {
            string sql = await File.ReadAllTextAsync(scriptPath);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await Container.DisposeAsync();
    }

    public NpgsqlConnection CreateConnection() => new(ConnectionString);
}

[CollectionDefinition(Name)]
public class PostgreSqlDatabaseCollection : ICollectionFixture<PostgreSqlTestFixture>
{
    public const string Name = "PostgreSqlDatabase";
}

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlTestFixture>
{
}
