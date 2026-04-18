using System.Threading.Channels;
using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs.Realtime;
using Graduation_Project_Backend.Service.Realtime;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Tests.TestSupport
{
    internal static class TestInfrastructure
    {
        public static AppDbContext CreateDbContext(string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
                .Options;

            return new AppDbContext(options);
        }
    }

    internal sealed class NoOpUserPointsUpdatesService : IUserPointsUpdatesService
    {
        public ValueTask PublishAsync(Guid userId, int totalPoints, string source, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ChannelReader<UserPointsChangedDto> Subscribe(Guid userId, CancellationToken cancellationToken = default)
            => Channel.CreateUnbounded<UserPointsChangedDto>().Reader;
    }
}
