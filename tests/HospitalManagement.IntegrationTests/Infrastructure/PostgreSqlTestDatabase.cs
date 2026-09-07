using Testcontainers.PostgreSql;

namespace HospitalManagement.IntegrationTests.Infrastructure;

internal sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    private const string TestImage = "postgres:18.6";
    private readonly PostgreSqlContainer _container;

    private PostgreSqlTestDatabase(PostgreSqlContainer container, string databaseName)
    {
        _container = container;
        DatabaseName = databaseName;
    }

    public string ConnectionString => _container.GetConnectionString();

    public string DatabaseName
    {
        get;
    }

    public static async Task<PostgreSqlTestDatabase> StartAsync()
    {
        var databaseName = $"hms_it_{Guid.NewGuid():N}";
        var container = new PostgreSqlBuilder(TestImage)
            .WithDatabase(databaseName)
            .WithUsername("hms_integration_test")
            .WithPassword("DEMO-TEST-ONLY-NOT-A-SECRET")
            .Build();

        try
        {
            await container.StartAsync();
            return new PostgreSqlTestDatabase(container, databaseName);
        }
        catch
        {
            await container.DisposeAsync();
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }
}
