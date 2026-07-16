namespace YakuMado.Core.Translation;

/// <summary>翻訳元言語と翻訳先言語の組み合わせ(例: en→ja)。</summary>
public readonly record struct LanguagePair(string SourceCode, string TargetCode)
{
    public override string ToString() => $"{SourceCode}->{TargetCode}";
}
