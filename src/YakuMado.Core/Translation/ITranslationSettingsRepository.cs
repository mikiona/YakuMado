namespace YakuMado.Core.Translation;

/// <summary>翻訳設定の永続化抽象化。APIキーの暗号化は実装(Infrastructure層)の責務とする。</summary>
public interface ITranslationSettingsRepository
{
    Task<TranslationSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(TranslationSettings settings, CancellationToken cancellationToken);
}
