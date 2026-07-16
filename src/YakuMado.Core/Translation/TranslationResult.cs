namespace YakuMado.Core.Translation;

/// <summary>翻訳エンジンからの翻訳結果。</summary>
public record TranslationResult(string TranslatedText, string EngineName, TimeSpan Elapsed);
