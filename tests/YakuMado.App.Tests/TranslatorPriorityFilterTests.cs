using YakuMado.App;
using YakuMado.Core.Translation;

namespace YakuMado.App.Tests;

public class TranslatorPriorityFilterTests
{
    private static readonly LanguagePair EnJa = new("en", "ja");

    [Fact]
    public void Apply_orders_translators_by_EnginePriorityOrder()
    {
        var translators = new ITranslator[]
        {
            new FakeSettingsTranslator("Azure", true, EnJa),
            new FakeSettingsTranslator("Local", true, EnJa),
        };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Local", "Azure" },
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string>());

        var result = TranslatorPriorityFilter.Apply(translators, settings);

        Assert.Equal(new[] { "Local", "Azure" }, result.Select(t => t.EngineName));
    }

    [Fact]
    public void Apply_appends_translators_not_present_in_priority_order_at_the_end()
    {
        var translators = new ITranslator[]
        {
            new FakeSettingsTranslator("Local", true, EnJa),
            new FakeSettingsTranslator("Azure", true, EnJa),
        };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Azure" },
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string>());

        var result = TranslatorPriorityFilter.Apply(translators, settings);

        Assert.Equal(new[] { "Azure", "Local" }, result.Select(t => t.EngineName));
    }

    [Fact]
    public void Apply_excludes_translators_disabled_in_EngineEnabled()
    {
        var translators = new ITranslator[]
        {
            new FakeSettingsTranslator("Local", true, EnJa),
            new FakeSettingsTranslator("Azure", true, EnJa),
        };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Local", "Azure" },
            EngineEnabled: new Dictionary<string, bool> { ["Azure"] = false },
            ApiKeys: new Dictionary<string, string>());

        var result = TranslatorPriorityFilter.Apply(translators, settings);

        Assert.Equal(new[] { "Local" }, result.Select(t => t.EngineName));
    }

    [Fact]
    public void Apply_defaults_to_enabled_when_not_present_in_EngineEnabled()
    {
        var translators = new ITranslator[] { new FakeSettingsTranslator("Local", true, EnJa) };
        var settings = new TranslationSettings(
            EnginePriorityOrder: Array.Empty<string>(),
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string>());

        var result = TranslatorPriorityFilter.Apply(translators, settings);

        Assert.Single(result);
    }
}
