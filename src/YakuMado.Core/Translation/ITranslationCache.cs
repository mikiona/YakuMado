namespace YakuMado.Core.Translation;

/// <summary>翻訳結果のキャッシュ抽象化。同一テキストの再翻訳を回避する。</summary>
public interface ITranslationCache
{
    bool TryGet(string normalizedKey, LanguagePair languagePair, out TranslationResult? result);

    void Set(string normalizedKey, LanguagePair languagePair, TranslationResult result);
}
