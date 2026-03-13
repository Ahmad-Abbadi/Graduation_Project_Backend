using System.Collections.Concurrent;
using System.Threading.Channels;
using Graduation_Project_Backend.DTOs.Realtime;

namespace Graduation_Project_Backend.Service.Realtime
{
    public sealed class UserPointsUpdatesService : IUserPointsUpdatesService
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<UserPointsChangedDto>>> _subscriptions = new();

        public ChannelReader<UserPointsChangedDto> Subscribe(Guid userId, CancellationToken cancellationToken = default)
        {
            var subscriptionId = Guid.NewGuid();
            Channel<UserPointsChangedDto> channel = Channel.CreateUnbounded<UserPointsChangedDto>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            ConcurrentDictionary<Guid, Channel<UserPointsChangedDto>> userSubscriptions =
                _subscriptions.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Channel<UserPointsChangedDto>>());

            userSubscriptions[subscriptionId] = channel;

            cancellationToken.Register(() => RemoveSubscription(userId, subscriptionId));

            return channel.Reader;
        }

        public ValueTask PublishAsync(Guid userId, int totalPoints, string source, CancellationToken cancellationToken = default)
        {
            if (!_subscriptions.TryGetValue(userId, out ConcurrentDictionary<Guid, Channel<UserPointsChangedDto>>? userSubscriptions))
                return ValueTask.CompletedTask;

            var update = new UserPointsChangedDto
            {
                UserId = userId,
                TotalPoints = totalPoints,
                Source = source,
                OccurredAtUtc = DateTime.UtcNow
            };

            List<Guid> failedSubscriptions = new();
            foreach ((Guid subscriptionId, Channel<UserPointsChangedDto> channel) in userSubscriptions)
            {
                if (!channel.Writer.TryWrite(update))
                    failedSubscriptions.Add(subscriptionId);
            }

            foreach (Guid subscriptionId in failedSubscriptions)
                RemoveSubscription(userId, subscriptionId);

            return ValueTask.CompletedTask;
        }

        private void RemoveSubscription(Guid userId, Guid subscriptionId)
        {
            if (!_subscriptions.TryGetValue(userId, out ConcurrentDictionary<Guid, Channel<UserPointsChangedDto>>? userSubscriptions))
                return;

            if (userSubscriptions.TryRemove(subscriptionId, out Channel<UserPointsChangedDto>? channel))
                channel.Writer.TryComplete();

            if (userSubscriptions.IsEmpty)
                _subscriptions.TryRemove(userId, out _);
        }
    }
}
