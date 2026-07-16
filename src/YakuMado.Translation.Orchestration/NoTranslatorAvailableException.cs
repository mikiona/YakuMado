namespace YakuMado.Translation.Orchestration;

/// <summary>指定された言語ペアに対応する利用可能な翻訳エンジンが1つも無かった場合に投げられる。</summary>
public sealed class NoTranslatorAvailableException : Exception
{
    public NoTranslatorAvailableException(string message) : base(message)
    {
    }
}
