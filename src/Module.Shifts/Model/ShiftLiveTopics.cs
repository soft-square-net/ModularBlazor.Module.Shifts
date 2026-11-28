using soft.square.net.ModularBlazor.Abstractions.Live;

namespace soft.square.net.Module.Shifts.Model;

/// <summary>
/// A page's live subscriptions. The module publishes <c>shift.changed</c> on the shift's topic
/// <c>shifts:shift:{id}</c> and on its unit's topic <c>unit:{id}</c> and every ancestor's (the names of
/// Shifts.Contracts' <c>ShiftsLive</c>; the client does not ship that package). Messages are nudges: they are gathered
/// for a moment and the page reloads once. A refused topic (no Organization module, no right) leaves the page with its
/// Refresh button.
/// </summary>
public sealed class ShiftLiveTopics(ILiveChannel? channel, Func<Task> changed, TimeSpan? gather = null) : IAsyncDisposable
{
    public const string ChangedMessage = "shift.changed";

    /// <summary>The most topics a page asks for: the hub limits a connection's subscriptions.</summary>
    public const int MaxTopics = 40;

    public static string Shift(int shiftId) => $"shifts:shift:{shiftId}";

    public static string Unit(Guid unitId) => $"unit:{unitId:N}";

    private readonly Dictionary<string, IAsyncDisposable> _subscriptions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _refused = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeSpan _gather = gather ?? TimeSpan.FromMilliseconds(300);
    private bool _pending;
    private bool _disposed;

    /// <summary>The topics the page is subscribed to.</summary>
    public IReadOnlyCollection<string> Topics => _subscriptions.Keys;

    /// <summary>True while the page hears about changes.</summary>
    public bool IsLive => _subscriptions.Keys.Any(t => !_refused.Contains(t)) && channel?.State == LiveState.Connected;

    /// <summary>
    /// Makes <paramref name="topics"/> the page's subscriptions: leaves the others and joins the new ones. Returns those
    /// of them that are refused.
    /// </summary>
    public async Task<IReadOnlyCollection<string>> SetAsync(IEnumerable<string> topics)
    {
        if (channel is null) return [];
        var wanted = topics.Distinct(StringComparer.Ordinal).Take(MaxTopics).ToArray();
        await _gate.WaitAsync();
        try
        {
            if (_disposed) return [];
            foreach (var topic in _subscriptions.Keys.Except(wanted).ToArray()) await LeaveAsync(topic);
            foreach (var topic in wanted)
            {
                if (_subscriptions.ContainsKey(topic) || _refused.Contains(topic)) continue;
                try
                {
                    _subscriptions[topic] = await channel.SubscribeAsync(topic, OnMessageAsync, options: new LiveSubscribeOptions
                    {
                        OnResync = _ => NudgeAsync(),
                        OnRevoked = _ => { _refused.Add(topic); return NudgeAsync(); },
                    });
                }
                catch (Exception e) when (e is LiveSubscriptionException or InvalidOperationException or HttpRequestException)
                {
                    _refused.Add(topic);
                }
            }
            return [.. wanted.Where(_refused.Contains)];
        }
        finally { _gate.Release(); }
    }

    private Task OnMessageAsync(LiveMessage message) => message.Type == ChangedMessage ? NudgeAsync() : Task.CompletedTask;

    private async Task NudgeAsync()
    {
        if (_pending || _disposed) return;
        _pending = true;
        try { await Task.Delay(_gather); }
        finally { _pending = false; }
        if (!_disposed) await changed();
    }

    private async Task LeaveAsync(string topic)
    {
        if (!_subscriptions.Remove(topic, out var subscription)) return;
        try { await subscription.DisposeAsync(); }
        catch (Exception) { /* the channel may be gone */ }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await _gate.WaitAsync();
        try
        {
            foreach (var topic in _subscriptions.Keys.ToArray()) await LeaveAsync(topic);
        }
        finally { _gate.Release(); }
    }
}
