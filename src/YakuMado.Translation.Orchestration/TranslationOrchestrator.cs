using YakuMado.Core.Translation;

namespace YakuMado.Translation.Orchestration;

/// <summary>
/// キャッシュ確認→優先順位順にIsAvailable/SupportedLanguagePairsでフィルタ→翻訳実行→キャッシュ書き込み、
/// という基本フローを実装する。サーキットブレーカーはPhase 5で追加予定。
/// </summary>
public sealed class TranslationOrchestrator : ITranslationOrchestrator
{
    private readonly IReadOnlyList<ITranslator> _translatorsByPriority;
    private readonly ITranslationCache _cache;

    public TranslationOrchestrator(IReadOnlyList<ITranslator> translatorsByPriority, ITranslationCache cache)
    {
        _translatorsByPriority = translatorsByPriority;
        _cache = cache;
    }

    public async Task<TranslationResult> TranslateAsync(
        string sourceText,
        LanguagePair languagePair,
        CancellationToken cancellationToken)
    {
        var normalizedKey = CacheKeyNormalizer.Normalize(sourceText);

        if (_cache.TryGet(normalizedKey, languagePair, out var cached) && cached != null)
        {
            return cached;
        }

        foreach (var translator in _translatorsByPriority)
        {
            if (!translator.IsAvailable) continue;
            if (!translator.SupportedLanguagePairs.Contains(languagePair)) continue;

            try
            {
                var result = await translator.TranslateAsync(sourceText, languagePair, cancellationToken);
                _cache.Set(normalizedKey, languagePair, result);
                return result;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // 次の候補へフォールバック
            }
        }

        throw new NoTranslatorAvailableException(
            $"言語ペア {languagePair} に対応する利用可能な翻訳エンジンがありません。");
    }
}
