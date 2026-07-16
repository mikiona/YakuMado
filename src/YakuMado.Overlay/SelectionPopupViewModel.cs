using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YakuMado.Overlay;

/// <summary>選択テキスト翻訳ポップアップのViewModel。View(WPF Window)からロジックを分離しテスト可能にする。</summary>
public sealed class SelectionPopupViewModel : INotifyPropertyChanged
{
    private string _translatedText = string.Empty;
    private bool _isVisible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string TranslatedText
    {
        get => _translatedText;
        private set => SetField(ref _translatedText, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetField(ref _isVisible, value);
    }

    public void Show(string translatedText)
    {
        TranslatedText = translatedText;
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
