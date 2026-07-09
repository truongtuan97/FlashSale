using FlashSale.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace FlashSale.IntegrationTests.Api;

internal sealed class PostgresFlashSaleApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _maintenanceConnectionString;
    private readonly string _databaseName;

    public PostgresFlashSaleApiFactory()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("FLASHSALE_TEST_POSTGRES");

        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException(
                "Set FLASHSALE_TEST_POSTGRES before running PostgreSQL integration tests.");
        }

        _databaseName = $"FlashSaleTests_{Guid.NewGuid():N}";

        var appConnectionBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = _databaseName
        };

        var maintenanceConnectionBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = "postgres"
        };

        _connectionString = appConnectionBuilder.ConnectionString;
        _maintenanceConnectionString = maintenanceConnectionBuilder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
         {
             services.RemoveAll<DbContextOptions>();
             services.RemoveAll<DbContextOptions<FlashSaleDbContext>>();
             services.RemoveAll<IDbContextOptionsConfiguration<FlashSaleDbContext>>();

             services.AddDbContext<FlashSaleDbContext>(Options => Options.UseNpgsql(_connectionString));

             using var serviceProvider = services.BuildServiceProvider();

             using var scope = serviceProvider.CreateScope();

             var dbContext = scope.ServiceProvider.GetRequiredService<FlashSaleDbContext>();

             dbContext.Database.Migrate();
         });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            DropDatabase();
        }
    }

    private void DropDatabase()
    {
        using var connection = new NpgsqlConnection(_maintenanceConnectionString);
        connection.Open();

        using var terminateCommand = connection.CreateCommand();
        terminateCommand.CommandText = $"""
        SELECT pg_terminate_backend(pid)
        FROM pg_stat_activity
        WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();
        """;
        terminateCommand.ExecuteNonQuery();

        using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"""DROP DATABASE IF EXISTS "{_databaseName}";""";
        dropCommand.ExecuteNonQuery();
    }
}