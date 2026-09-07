using HospitalManagement.IntegrationTests.Infrastructure;

using Microsoft.Extensions.Logging;

namespace HospitalManagement.IntegrationTests;

public sealed class ApiFoundationFixture : IAsyncLifetime, IDisposable
{
    private PostgreSqlTestDatabase? _database;
    private ApiWebApplicationFactory? _factory;

    internal ApiWebApplicationFactory Factory => _factory
        ?? throw new InvalidOperationException("The API test fixture has not been initialized.");

    public async Task InitializeAsync()
    {
        _database = await PostgreSqlTestDatabase.StartAsync();
        _factory = new ApiWebApplicationFactory(_database.ConnectionString);
    }

    internal ApiWebApplicationFactory CreateRateLimitedFactory(int permitLimit)
    {
        var database = _database
            ?? throw new InvalidOperationException("The API test fixture has not been initialized.");

        return new ApiWebApplicationFactory(database.ConnectionString, permitLimit);
    }

    internal ApiWebApplicationFactory CreateObservabilityFactory(ILoggerProvider loggerProvider)
    {
        ArgumentNullException.ThrowIfNull(loggerProvider);

        var database = _database
            ?? throw new InvalidOperationException("The API test fixture has not been initialized.");

        return new ApiWebApplicationFactory(
            database.ConnectionString,
            loggerProvider: loggerProvider);
    }

    public async Task DisposeAsync()
    {
        Dispose();

        if (_database is not null)
        {
            await _database.DisposeAsync();
            _database = null;
        }
    }

    public void Dispose()
    {
        _factory?.Dispose();
        _factory = null;
    }
}
