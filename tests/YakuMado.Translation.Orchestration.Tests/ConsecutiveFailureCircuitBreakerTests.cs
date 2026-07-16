using YakuMado.Translation.Orchestration;

namespace YakuMado.Translation.Orchestration.Tests;

public class ConsecutiveFailureCircuitBreakerTests
{
    [Fact]
    public void IsOpen_is_false_for_engine_with_no_recorded_activity()
    {
        var breaker = new ConsecutiveFailureCircuitBreaker(failureThreshold: 3);

        Assert.False(breaker.IsOpen("EngineA"));
    }

    [Fact]
    public void IsOpen_is_false_when_failures_below_threshold()
    {
        var breaker = new ConsecutiveFailureCircuitBreaker(failureThreshold: 3);

        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");

        Assert.False(breaker.IsOpen("EngineA"));
    }

    [Fact]
    public void IsOpen_becomes_true_when_failures_reach_threshold()
    {
        var breaker = new ConsecutiveFailureCircuitBreaker(failureThreshold: 3);

        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");

        Assert.True(breaker.IsOpen("EngineA"));
    }

    [Fact]
    public void RecordSuccess_resets_consecutive_failure_count()
    {
        var breaker = new ConsecutiveFailureCircuitBreaker(failureThreshold: 3);

        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");
        breaker.RecordSuccess("EngineA");
        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");

        Assert.False(breaker.IsOpen("EngineA"));
    }

    [Fact]
    public void IsOpen_closes_again_after_open_duration_elapses()
    {
        var currentTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var breaker = new ConsecutiveFailureCircuitBreaker(
            failureThreshold: 3,
            openDuration: TimeSpan.FromSeconds(30),
            clock: () => currentTime);

        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");
        Assert.True(breaker.IsOpen("EngineA"));

        currentTime = currentTime.AddSeconds(31);

        Assert.False(breaker.IsOpen("EngineA"));
    }

    [Fact]
    public void IsOpen_remains_true_before_open_duration_elapses()
    {
        var currentTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var breaker = new ConsecutiveFailureCircuitBreaker(
            failureThreshold: 3,
            openDuration: TimeSpan.FromSeconds(30),
            clock: () => currentTime);

        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");

        currentTime = currentTime.AddSeconds(10);

        Assert.True(breaker.IsOpen("EngineA"));
    }

    [Fact]
    public void IsOpen_is_tracked_independently_per_engine()
    {
        var breaker = new ConsecutiveFailureCircuitBreaker(failureThreshold: 2);

        breaker.RecordFailure("EngineA");
        breaker.RecordFailure("EngineA");

        Assert.True(breaker.IsOpen("EngineA"));
        Assert.False(breaker.IsOpen("EngineB"));
    }
}
