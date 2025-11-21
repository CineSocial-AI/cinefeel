using CineSocial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CineSocial.Infrastructure.Repositories;

namespace CineSocial.Application.UnitTests.Common;

/// <summary>
/// Factory for creating test databases using EF Core In-Memory provider
/// </summary>
public static class TestDatabaseFactory
{
    public static ApplicationDbContext CreateInMemoryDatabase(string databaseName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName + Guid.NewGuid())
            .Options;

        var context = new ApplicationDbContext(options);
        return context;
    }

    public static UnitOfWork CreateUnitOfWork(ApplicationDbContext context)
    {
        return new UnitOfWork(context);
    }
}
