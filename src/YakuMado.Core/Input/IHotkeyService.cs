namespace YakuMado.Core.Input;

/// <summary>グローバルホットキー登録の抽象化。</summary>
public interface IHotkeyService
{
    event EventHandler? SelectionTranslateRequested;

    void Register();

    void Unregister();
}
