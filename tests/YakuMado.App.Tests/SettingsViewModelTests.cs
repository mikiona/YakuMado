using YakuMado.App;
using YakuMado.Core.Translation;

namespace YakuMado.App.Tests;

public sealed class FakeSettingsTranslator : ITranslator
{
    public string EngineName { get; }
    public bool IsAvailable { get; set; }
    public IReadOnlyCollection<LanguagePair> SupportedLanguagePairs { get; }

    public FakeSettingsTranslator(string engineName, bool isAvailable, params LanguagePair[] pairs)
    {
        EngineName = engineName;
        IsAvailable = isAvailable;
        SupportedLanguagePairs = pairs;
    }

    public Task<TranslationResult> TranslateAsync(string sourceText, LanguagePair languagePair, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}

public class SettingsViewModelTests
{
    private static readonly LanguagePair EnJa = new("en", "ja");

    [Fact]
    public void Constructor_creates_one_engine_row_per_translator_in_priority_order()
    {
        var translators = new ITranslator[]
        {
            new FakeSettingsTranslator("Local", true, EnJa),
            new FakeSettingsTranslator("Azure", false, EnJa),
        };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Local", "Azure" },
            EngineEnabled: new Dictionary<string, bool> { ["Local"] = true, ["Azure"] = true },
            ApiKeys: new Dictionary<string, string>());

        var vm = new SettingsViewModel(settings, translators);

        Assert.Equal(2, vm.Engines.Count);
        Assert.Equal("Local", vm.Engines[0].EngineName);
        Assert.Equal("Azure", vm.Engines[1].EngineName);
    }

    [Fact]
    public void Constructor_reflects_IsAvailable_and_IsEnabled_per_engine()
    {
        var translators = new ITranslator[]
        {
            new FakeSettingsTranslator("Local", true, EnJa),
            new FakeSettingsTranslator("Azure", false, EnJa),
        };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Local", "Azure" },
            EngineEnabled: new Dictionary<string, bool> { ["Local"] = true, ["Azure"] = false },
            ApiKeys: new Dictionary<string, string>());

        var vm = new SettingsViewModel(settings, translators);

        Assert.True(vm.Engines[0].IsAvailable);
        Assert.True(vm.Engines[0].IsEnabled);
        Assert.False(vm.Engines[1].IsAvailable);
        Assert.False(vm.Engines[1].IsEnabled);
    }

    [Fact]
    public void Constructor_defaults_IsEnabled_to_true_when_not_present_in_settings()
    {
        var translators = new ITranslator[] { new FakeSettingsTranslator("NewEngine", true, EnJa) };
        var settings = new TranslationSettings(
            EnginePriorityOrder: Array.Empty<string>(),
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string>());

        var vm = new SettingsViewModel(settings, translators);

        Assert.True(vm.Engines[0].IsEnabled);
    }

    [Fact]
    public void Constructor_appends_translators_not_present_in_priority_order_at_the_end()
    {
        var translators = new ITranslator[]
        {
            new FakeSettingsTranslator("Local", true, EnJa),
            new FakeSettingsTranslator("Azure", true, EnJa),
        };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Azure" }, // "Local"は優先順位リストに無い
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string>());

        var vm = new SettingsViewModel(settings, translators);

        Assert.Equal("Azure", vm.Engines[0].EngineName);
        Assert.Equal("Local", vm.Engines[1].EngineName);
    }

    [Fact]
    public void MoveEngineUp_swaps_with_previous_engine()
    {
        var translators = new ITranslator[]
        {
            new FakeSettingsTranslator("Local", true, EnJa),
            new FakeSettingsTranslator("Azure", true, EnJa),
        };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Local", "Azure" },
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string>());
        var vm = new SettingsViewModel(settings, translators);

        vm.MoveEngineUp("Azure");

        Assert.Equal("Azure", vm.Engines[0].EngineName);
        Assert.Equal("Local", vm.Engines[1].EngineName);
    }

    [Fact]
    public void MoveEngineUp_does_nothing_when_already_first()
    {
        var translators = new ITranslator[] { new FakeSettingsTranslator("Local", true, EnJa) };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Local" },
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string>());
        var vm = new SettingsViewModel(settings, translators);

        vm.MoveEngineUp("Local");

        Assert.Equal("Local", vm.Engines[0].EngineName);
    }

    [Fact]
    public void MoveEngineDown_swaps_with_next_engine()
    {
        var translators = new ITranslator[]
        {
            new FakeSettingsTranslator("Local", true, EnJa),
            new FakeSettingsTranslator("Azure", true, EnJa),
        };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Local", "Azure" },
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string>());
        var vm = new SettingsViewModel(settings, translators);

        vm.MoveEngineDown("Local");

        Assert.Equal("Azure", vm.Engines[0].EngineName);
        Assert.Equal("Local", vm.Engines[1].EngineName);
    }

    [Fact]
    public void ToTranslationSettings_reflects_current_order_enabled_state_and_api_key()
    {
        var translators = new ITranslator[]
        {
            new FakeSettingsTranslator("Local", true, EnJa),
            new FakeSettingsTranslator("Azure", true, EnJa),
        };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Local", "Azure" },
            EngineEnabled: new Dictionary<string, bool> { ["Local"] = true, ["Azure"] = true },
            ApiKeys: new Dictionary<string, string>());
        var vm = new SettingsViewModel(settings, translators);

        vm.MoveEngineUp("Azure");
        vm.Engines.Single(e => e.EngineName == "Local").IsEnabled = false;
        vm.AzureApiKey = "my-secret-key";
        vm.AzureRegion = "japaneast";

        var result = vm.ToTranslationSettings();

        Assert.Equal(new[] { "Azure", "Local" }, result.EnginePriorityOrder);
        Assert.False(result.EngineEnabled["Local"]);
        Assert.True(result.EngineEnabled["Azure"]);
        Assert.Equal("my-secret-key", result.ApiKeys["Azure"]);
    }

    [Fact]
    public void Constructor_loads_existing_azure_api_key_from_settings()
    {
        var translators = new ITranslator[] { new FakeSettingsTranslator("Azure", true, EnJa) };
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Azure" },
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string> { ["Azure"] = "existing-key" });

        var vm = new SettingsViewModel(settings, translators);

        Assert.Equal("existing-key", vm.AzureApiKey);
    }
}
