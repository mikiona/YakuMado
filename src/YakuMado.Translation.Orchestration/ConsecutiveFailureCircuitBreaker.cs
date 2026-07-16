namespace YakuMado.Translation.Orchestration;

/// <summary>
/// 連続失敗回数が閾値に達したエンジンを一定時間(openDuration)候補から除外するサーキットブレーカー。
/// openDuration経過後は再度候補に戻り(半開状態)、次に失敗すれば再び除外される。
/// </summary>
public sealed class ConsecutiveFailureCircuitBreaker : ICircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Dictionary<string, State> _states = new();
    private readonly object _lock = new();

    public ConsecutiveFailureCircuitBreaker(
        int failureThreshold = 3,
        TimeSpan? openDuration = null,
        Func<DateTimeOffset>? clock = null)
    {
        if (failureThreshold <= 0) throw new ArgumentOutOfRangeException(nameof(failureThreshold));

        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsOpen(string engineName)
    {
        lock (_lock)
        {
            if (!_states.TryGetValue(engineName, out var state) || state.OpenedAt == null)
            {
                return false;
            }

            if (_clock() - state.OpenedAt.Value >= _openDuration)
            {
                _states[engineName] = new State(0, null);
                return false;
            }

            return true;
        }
    }

    public void RecordFailure(string engineName)
    {
        lock (_lock)
        {
            var previous = _states.TryGetValue(engineName, out var s) ? s : new State(0, null);
            var consecutiveFailures = previous.ConsecutiveFailures + 1;
            var openedAt = consecutiveFailures >= _failureThreshold ? _clock() : previous.OpenedAt;

            _states[engineName] = new State(consecutiveFailures, openedAt);
        }
    }

    public void RecordSuccess(string engineName)
    {
        lock (_lock)
        {
            _states[engineName] = new State(0, null);
        }
    }

    private sealed record State(int ConsecutiveFailures, DateTimeOffset? OpenedAt);
}
