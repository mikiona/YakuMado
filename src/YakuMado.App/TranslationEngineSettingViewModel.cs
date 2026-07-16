using System.ComponentModel;
using System.Runtime.CompilerServices;
using YakuMado.Core.Translation;

namespace YakuMado.App;

/// <summary>設定画面における1翻訳エンジン分の行を表すViewModel。</summary>
public sealed class TranslationEngineSettingViewModel : INotifyPropertyChanged
{
    private bool _isEnabled;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string EngineName { get; }

    public bool IsAvailable { get; }

    public IReadOnlyCollection<LanguagePair> SupportedLanguagePairs { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }

    public TranslationEngineSettingViewModel(
        string engineName, bool isAvailable, bool isEnabled, IReadOnlyCollection<LanguagePair> supportedLanguagePairs)
    {
        EngineName = engineName;
        IsAvailable = isAvailable;
        _isEnabled = isEnabled;
        SupportedLanguagePairs = supportedLanguagePairs;
    }
}
