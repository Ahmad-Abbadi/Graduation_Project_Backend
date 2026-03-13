using System.Threading.Channels;
using Graduation_Project_Backend.DTOs.Realtime;

namespace Graduation_Project_Backend.Service.Realtime
{
    public interface IUserPointsUpdatesService
    {
        ChannelReader<UserPointsChangedDto> Subscribe(Guid userId, CancellationToken cancellationToken = default);
        ValueTask PublishAsync(Guid userId, int totalPoints, string source, CancellationToken cancellationToken = default);
    }
}
