namespace YakuMado.Core.Translation;

/// <summary>翻訳エンジンの設定。優先順位・有効/無効・APIキーを保持する。</summary>
public record TranslationSettings(
    IReadOnlyList<string> EnginePriorityOrder,
    IReadOnlyDictionary<string, bool> EngineEnabled,
    IReadOnlyDictionary<string, string> ApiKeys);
