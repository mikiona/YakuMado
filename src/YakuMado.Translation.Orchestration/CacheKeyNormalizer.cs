using System.Text;

namespace YakuMado.Translation.Orchestration;

/// <summary>キャッシュキー用にテキストを正規化する(前後空白除去・改行/連続空白の統一)。</summary>
public static class CacheKeyNormalizer
{
    public static string Normalize(string text)
    {
        var trimmed = text.Trim();
        var builder = new StringBuilder(trimmed.Length);
        bool lastWasWhitespace = false;

        foreach (var ch in trimmed)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasWhitespace)
                {
                    builder.Append(' ');
                    lastWasWhitespace = true;
                }
            }
            else
            {
                builder.Append(ch);
                lastWasWhitespace = false;
            }
        }

        return builder.ToString();
    }
}
