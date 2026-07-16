namespace YakuMado.Translation.Orchestration;

/// <summary>翻訳エンジンが連続失敗した場合に一定時間候補から除外するサーキットブレーカーの抽象化。</summary>
public interface ICircuitBreaker
{
    bool IsOpen(string engineName);

    void RecordFailure(string engineName);

    void RecordSuccess(string engineName);
}
