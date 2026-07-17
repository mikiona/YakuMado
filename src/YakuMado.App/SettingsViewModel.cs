using System.Collections.ObjectModel;
using YakuMado.Core.Translation;

namespace YakuMado.App;

/// <summary>
/// 設定画面(F6: エンジン優先順位/有効無効、F7: APIキー、F9: 対応言語表示)のViewModel。
/// TranslationSettingsと登録済みITranslator一覧から表示用の行を組み立て、
/// 編集後は再びTranslationSettingsへ変換して永続化できるようにする。
/// </summary>
public sealed class SettingsViewModel
{
    public ObservableCollection<TranslationEngineSettingViewModel> Engines { get; } = new();

    public string AzureApiKey { get; set; } = string.Empty;

    public string AzureRegion { get; set; } = string.Empty;

    public SettingsViewModel(TranslationSettings initialSettings, IReadOnlyList<ITranslator> translators)
    {
        var translatorsByName = translators.ToDictionary(t => t.EngineName);
        var orderedNames = new List<string>(initialSettings.EnginePriorityOrder);

        // 優先順位リストに無い翻訳エンジン(新規登録分等)は末尾に追加する
        foreach (var translator in translators)
        {
            if (!orderedNames.Contains(translator.EngineName))
            {
                orderedNames.Add(translator.EngineName);
            }
        }

        foreach (var name in orderedNames)
        {
            if (!translatorsByName.TryGetValue(name, out var translator)) continue;

            var isEnabled = !initialSettings.EngineEnabled.TryGetValue(name, out var enabled) || enabled;
            Engines.Add(new TranslationEngineSettingViewModel(
                translator.EngineName, translator.IsAvailable, isEnabled, translator.SupportedLanguagePairs));
        }

        AzureApiKey = initialSettings.ApiKeys.GetValueOrDefault("Azure", string.Empty);
        AzureRegion = initialSettings.ApiKeys.GetValueOrDefault("AzureRegion", string.Empty);
    }

    public void MoveEngineUp(string engineName)
    {
        var index = IndexOf(engineName);
        if (index <= 0) return;
        Swap(index, index - 1);
    }

    public void MoveEngineDown(string engineName)
    {
        var index = IndexOf(engineName);
        if (index < 0 || index >= Engines.Count - 1) return;
        Swap(index, index + 1);
    }

    public TranslationSettings ToTranslationSettings()
    {
        var priorityOrder = Engines.Select(e => e.EngineName).ToArray();
        var enabledMap = Engines.ToDictionary(e => e.EngineName, e => e.IsEnabled);
        var apiKeys = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(AzureApiKey))
        {
            apiKeys["Azure"] = AzureApiKey;
        }
        if (!string.IsNullOrEmpty(AzureRegion))
        {
            apiKeys["AzureRegion"] = AzureRegion;
        }

        return new TranslationSettings(priorityOrder, enabledMap, apiKeys);
    }

    private int IndexOf(string engineName)
    {
        for (int i = 0; i < Engines.Count; i++)
        {
            if (Engines[i].EngineName == engineName) return i;
        }
        return -1;
    }

    private void Swap(int indexA, int indexB)
    {
        (Engines[indexA], Engines[indexB]) = (Engines[indexB], Engines[indexA]);
    }
}
