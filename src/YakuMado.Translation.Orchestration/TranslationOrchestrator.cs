using YakuMado.Core.Translation;

namespace YakuMado.Translation.Orchestration;

/// <summary>
/// キャッシュ確認→優先順位順にIsAvailable/SupportedLanguagePairs/サーキットブレーカーでフィルタ→
/// 翻訳実行→キャッシュ書き込み、という基本フローを実装する。
/// </summary>
public sealed class TranslationOrchestrator : ITranslationOrchestrator
{
    private readonly IReadOnlyList<ITranslator> _translatorsByPriority;
    private readonly ITranslationCache _cache;
    private readonly ICircuitBreaker _circuitBreaker;

    public TranslationOrchestrator(
        IReadOnlyList<ITranslator> translatorsByPriority,
        ITranslationCache cache,
        ICircuitBreaker circuitBreaker)
    {
        _translatorsByPriority = translatorsByPriority;
        _cache = cache;
        _circuitBreaker = circuitBreaker;
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
            if (_circuitBreaker.IsOpen(translator.EngineName)) continue;

            try
            {
                var result = await translator.TranslateAsync(sourceText, languagePair, cancellationToken);
                _circuitBreaker.RecordSuccess(translator.EngineName);
                _cache.Set(normalizedKey, languagePair, result);
                return result;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                _circuitBreaker.RecordFailure(translator.EngineName);
                // 次の候補へフォールバック
            }
        }

        throw new NoTranslatorAvailableException(
            $"言語ペア {languagePair} に対応する利用可能な翻訳エンジンがありません。");
    }
}
