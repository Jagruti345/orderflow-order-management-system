using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Tests.Unit;

public static class TestDb
{
    // A unique database name per call means tests never share data
    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}