using YakuMado.Core.Translation;
using YakuMado.Translation.Orchestration;

namespace YakuMado.Translation.Orchestration.Tests;

public class LruTranslationCacheTests
{
    private static readonly LanguagePair EnJa = new("en", "ja");

    [Fact]
    public void TryGet_returns_false_when_key_not_present()
    {
        var cache = new LruTranslationCache(capacity: 10);

        var found = cache.TryGet("hello", EnJa, out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void Set_then_TryGet_returns_the_stored_result()
    {
        var cache = new LruTranslationCache(capacity: 10);
        var expected = new TranslationResult("こんにちは", "Fake", TimeSpan.FromMilliseconds(10));

        cache.Set("hello", EnJa, expected);
        var found = cache.TryGet("hello", EnJa, out var result);

        Assert.True(found);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryGet_distinguishes_by_language_pair()
    {
        var cache = new LruTranslationCache(capacity: 10);
        var enJaResult = new TranslationResult("こんにちは", "Fake", TimeSpan.Zero);
        cache.Set("hello", EnJa, enJaResult);

        var found = cache.TryGet("hello", new LanguagePair("en", "fr"), out var result);

        Assert.False(found);
    }

    [Fact]
    public void Set_evicts_least_recently_used_entry_when_capacity_exceeded()
    {
        var cache = new LruTranslationCache(capacity: 2);
        cache.Set("a", EnJa, new TranslationResult("A", "Fake", TimeSpan.Zero));
        cache.Set("b", EnJa, new TranslationResult("B", "Fake", TimeSpan.Zero));
        cache.Set("c", EnJa, new TranslationResult("C", "Fake", TimeSpan.Zero)); // "a"が追い出されるはず

        Assert.False(cache.TryGet("a", EnJa, out _));
        Assert.True(cache.TryGet("b", EnJa, out _));
        Assert.True(cache.TryGet("c", EnJa, out _));
    }

    [Fact]
    public void TryGet_marks_entry_as_recently_used_preventing_eviction()
    {
        var cache = new LruTranslationCache(capacity: 2);
        cache.Set("a", EnJa, new TranslationResult("A", "Fake", TimeSpan.Zero));
        cache.Set("b", EnJa, new TranslationResult("B", "Fake", TimeSpan.Zero));

        cache.TryGet("a", EnJa, out _); // "a"を最近使用済みにする

        cache.Set("c", EnJa, new TranslationResult("C", "Fake", TimeSpan.Zero)); // "b"が追い出されるはず

        Assert.True(cache.TryGet("a", EnJa, out _));
        Assert.False(cache.TryGet("b", EnJa, out _));
        Assert.True(cache.TryGet("c", EnJa, out _));
    }
}
