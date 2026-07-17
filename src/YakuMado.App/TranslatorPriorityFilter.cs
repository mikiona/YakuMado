using YakuMado.Core.Translation;

namespace YakuMado.App;

/// <summary>
/// TranslationSettingsの優先順位・有効/無効設定を実際のITranslator一覧に適用する。
/// 優先順位リストに無いエンジンは末尾に追加し、無効化されたエンジンは除外する
/// (SettingsViewModelの表示ロジックと同じ規則)。
/// </summary>
public static class TranslatorPriorityFilter
{
    public static IReadOnlyList<ITranslator> Apply(IReadOnlyList<ITranslator> translators, TranslationSettings settings)
    {
        var byName = translators.ToDictionary(t => t.EngineName);
        var orderedNames = new List<string>(settings.EnginePriorityOrder);
        foreach (var translator in translators)
        {
            if (!orderedNames.Contains(translator.EngineName))
            {
                orderedNames.Add(translator.EngineName);
            }
        }

        var result = new List<ITranslator>();
        foreach (var name in orderedNames)
        {
            if (!byName.TryGetValue(name, out var translator)) continue;

            var isEnabled = !settings.EngineEnabled.TryGetValue(name, out var enabled) || enabled;
            if (isEnabled)
            {
                result.Add(translator);
            }
        }

        return result;
    }
}
