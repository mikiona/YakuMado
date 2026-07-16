namespace YakuMado.Ocr;

/// <summary>
/// 要求言語タグ(例: "en")が、OSにインストール済みの言語タグ一覧(例: "en-US")に
/// 一致するかどうかを判定する純粋ロジック。Windowsの言語パックは地域付きタグで
/// 登録されることが多いため、ベース言語コードでの前方一致を許容する。
/// </summary>
public static class OcrLanguageAvailabilityChecker
{
    public static bool IsLanguageAvailable(string requestedLanguageTag, IEnumerable<string> availableLanguageTags)
    {
        foreach (var available in availableLanguageTags)
        {
            if (available.Equals(requestedLanguageTag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (available.StartsWith(requestedLanguageTag + "-", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
