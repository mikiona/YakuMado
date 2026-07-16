using YakuMado.Core.Translation;

namespace YakuMado.Translation.Orchestration;

public interface ITranslationOrchestrator
{
    Task<TranslationResult> TranslateAsync(
        string sourceText,
        LanguagePair languagePair,
        CancellationToken cancellationToken);
}
