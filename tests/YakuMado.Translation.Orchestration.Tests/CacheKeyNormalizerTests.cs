using YakuMado.Translation.Orchestration;

namespace YakuMado.Translation.Orchestration.Tests;

public class CacheKeyNormalizerTests
{
    [Theory]
    [InlineData("  hello  ", "hello")]
    [InlineData("hello\nworld", "hello world")]
    [InlineData("hello   world", "hello world")]
    [InlineData("hello\t\tworld", "hello world")]
    [InlineData("", "")]
    public void Normalize_trims_and_collapses_whitespace(string input, string expected)
    {
        Assert.Equal(expected, CacheKeyNormalizer.Normalize(input));
    }
}
