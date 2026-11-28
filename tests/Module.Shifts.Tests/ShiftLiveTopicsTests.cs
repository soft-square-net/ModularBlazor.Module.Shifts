using soft.square.net.ModularBlazor.Abstractions.Live;

namespace soft.square.net.Module.Shifts.Tests;

public class ShiftLiveTopicsTests
{
    private sealed class Channel : ILiveChannel
    {
        public Dictionary<string, (Func<LiveMessage, Task> Handler, LiveSubscribeOptions? Options)> Joined { get; } = [];
        public HashSet<string> Forbidden { get; } = [];
        public LiveState State => LiveState.Connected;
        public event Action<LiveState>? StateChanged { add { } remove { } }
        public event Func<LiveResync, Task>? Resync { add { } remove { } }

        public Task<IAsyncDisposable> SubscribeAsync(string topic, Func<LiveMessage, Task> handler, long? fromSequence = null,
            LiveSubscribeOptions? options = null, CancellationToken ct = default)
        {
            if (Forbidden.Contains(topic)) throw new LiveSubscriptionException(topic, "forbidden");
            Joined[topic] = (handler, options);
            return Task.FromResult<IAsyncDisposable>(new Leave(this, topic));
        }

        public Task SendAsync(string topic, string type = ShiftLiveTopics.ChangedMessage) =>
            Joined[topic].Handler(new LiveMessage(topic, type, null, null, DateTimeOffset.UtcNow));

        private sealed class Leave(Channel channel, string topic) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                channel.Joined.Remove(topic);
                return ValueTask.CompletedTask;
            }
        }
    }

    [Fact]
    public void Topics_are_named_as_the_module_publishes_them()
    {
        ShiftLiveTopics.Shift(42).ShouldBe("shifts:shift:42");
        ShiftLiveTopics.Unit(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e")).ShouldBe("unit:0f8fad5bd9cb469fa16570867728950e");
    }

    [Fact]
    public async Task Setting_the_topics_leaves_the_others()
    {
        var channel = new Channel();
        await using var live = new ShiftLiveTopics(channel, () => Task.CompletedTask, TimeSpan.Zero);

        await live.SetAsync(["a", "b"]);
        await live.SetAsync(["b", "c"]);

        channel.Joined.Keys.ShouldBe(["b", "c"], ignoreOrder: true);
        live.IsLive.ShouldBeTrue();
    }

    [Fact]
    public async Task A_refused_topic_is_reported_and_not_asked_for_again()
    {
        var channel = new Channel();
        channel.Forbidden.Add("unit:x");
        await using var live = new ShiftLiveTopics(channel, () => Task.CompletedTask, TimeSpan.Zero);

        (await live.SetAsync(["unit:x"])).ShouldBe(["unit:x"]);
        channel.Forbidden.Clear();
        (await live.SetAsync(["unit:x"])).ShouldBe(["unit:x"]);

        channel.Joined.ShouldBeEmpty();
    }

    [Fact]
    public async Task Messages_that_arrive_together_reload_once_and_other_messages_are_ignored()
    {
        var channel = new Channel();
        var reloads = 0;
        await using var live = new ShiftLiveTopics(channel, () => { reloads++; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(100));
        await live.SetAsync(["a", "b"]);

        await channel.SendAsync("a", "something.else");
        await Task.WhenAll(channel.SendAsync("a"), channel.SendAsync("b"), channel.SendAsync("a"));

        reloads.ShouldBe(1);
    }

    [Fact]
    public async Task Disposing_leaves_every_topic()
    {
        var channel = new Channel();
        var live = new ShiftLiveTopics(channel, () => Task.CompletedTask, TimeSpan.Zero);
        await live.SetAsync(["a"]);

        await live.DisposeAsync();
        (await live.SetAsync(["b"])).ShouldBeEmpty();

        channel.Joined.ShouldBeEmpty();
    }

    [Fact]
    public async Task Without_a_live_channel_nothing_happens()
    {
        await using var live = new ShiftLiveTopics(null, () => Task.CompletedTask);

        (await live.SetAsync(["a"])).ShouldBeEmpty();
        live.IsLive.ShouldBeFalse();
    }
}
