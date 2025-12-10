using Healthcare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Healthcare.Application.UnitTests;

public static class TestDbContextFactory
{
    public static HealthcareDbContext Create()
    {
        var options = new DbContextOptionsBuilder<HealthcareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthcareDbContext(options);
    }
}

