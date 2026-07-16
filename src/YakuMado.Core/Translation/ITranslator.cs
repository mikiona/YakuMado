namespace YakuMado.Core.Translation;

/// <summary>翻訳エンジンの抽象化。ローカルNMT・クラウドAPIの両方をこのインターフェースの実装として差し替え可能にする。</summary>
public interface ITranslator
{
    string EngineName { get; }

    /// <summary>現在利用可能かどうかの自己申告。モデル未配置・APIキー未設定・ネットワーク不可等でfalseを返す。</summary>
    bool IsAvailable { get; }

    IReadOnlyCollection<LanguagePair> SupportedLanguagePairs { get; }

    Task<TranslationResult> TranslateAsync(
        string sourceText,
        LanguagePair languagePair,
        CancellationToken cancellationToken);
}
