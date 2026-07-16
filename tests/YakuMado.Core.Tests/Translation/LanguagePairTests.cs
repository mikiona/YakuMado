using YakuMado.Core.Translation;

namespace YakuMado.Core.Tests.Translation;

public class LanguagePairTests
{
    [Fact]
    public void ToString_formats_as_source_arrow_target()
    {
        var pair = new LanguagePair("en", "ja");

        Assert.Equal("en->ja", pair.ToString());
    }

    [Fact]
    public void Equality_is_value_based()
    {
        var a = new LanguagePair("en", "ja");
        var b = new LanguagePair("en", "ja");

        Assert.Equal(a, b);
    }
}
