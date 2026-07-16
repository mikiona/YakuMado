using YakuMado.Ocr;

namespace YakuMado.Ocr.Tests;

public class OcrLanguageAvailabilityCheckerTests
{
    [Fact]
    public void IsLanguageAvailable_returns_true_for_exact_match()
    {
        var result = OcrLanguageAvailabilityChecker.IsLanguageAvailable("en", new[] { "ja", "en" });

        Assert.True(result);
    }

    [Fact]
    public void IsLanguageAvailable_returns_false_when_not_present()
    {
        var result = OcrLanguageAvailabilityChecker.IsLanguageAvailable("en", new[] { "ja", "fr" });

        Assert.False(result);
    }

    [Fact]
    public void IsLanguageAvailable_matches_region_specific_tags_case_insensitively()
    {
        // Windowsの言語パックは "en-US" のように地域付きで登録されることが多いため、
        // ベース言語コード("en")のみでの要求にも一致させる
        var result = OcrLanguageAvailabilityChecker.IsLanguageAvailable("en", new[] { "EN-US" });

        Assert.True(result);
    }

    [Fact]
    public void IsLanguageAvailable_returns_false_for_empty_available_list()
    {
        var result = OcrLanguageAvailabilityChecker.IsLanguageAvailable("en", Array.Empty<string>());

        Assert.False(result);
    }
}
