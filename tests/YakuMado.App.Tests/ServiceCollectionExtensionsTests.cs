using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using YakuMado.App;
using YakuMado.Core.TextAcquisition;
using YakuMado.Core.Translation;
using YakuMado.Overlay;
using YakuMado.Translation.Orchestration;

namespace YakuMado.App.Tests;

public class ServiceCollectionExtensionsTests
{
    private static readonly TranslationSettings EmptySettings = new(
        EnginePriorityOrder: Array.Empty<string>(),
        EngineEnabled: new Dictionary<string, bool>(),
        ApiKeys: new Dictionary<string, string>());

    [Fact]
    public void AddYakuMadoCore_registers_ITranslationSettingsRepository()
    {
        var services = new ServiceCollection();

        services.AddYakuMadoCore(settingsFilePath: "dummy-path.json");
        var provider = services.BuildServiceProvider();

        var repository = provider.GetService<ITranslationSettingsRepository>();

        Assert.NotNull(repository);
    }

    [Fact]
    public void AddYakuMadoCore_registers_ITranslationSettingsRepository_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddYakuMadoCore(settingsFilePath: "dummy-path.json");
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<ITranslationSettingsRepository>();
        var second = provider.GetRequiredService<ITranslationSettingsRepository>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddSelectionTranslationFeature_resolves_all_key_services_without_throwing()
    {
        var services = new ServiceCollection();

        services.AddSelectionTranslationFeature(EmptySettings);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ISelectionAcquisitionChain>());
        Assert.NotNull(provider.GetRequiredService<ITranslationOrchestrator>());
        Assert.NotNull(provider.GetRequiredService<ITranslator>());
        Assert.NotNull(provider.GetRequiredService<SelectionPopupViewModel>());
    }

    [Fact]
    public void AddSelectionTranslationFeature_translator_is_unavailable_without_api_key_env_var()
    {
        Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", null);
        var services = new ServiceCollection();

        services.AddSelectionTranslationFeature(EmptySettings);
        var provider = services.BuildServiceProvider();

        var translator = provider.GetRequiredService<ITranslator>();
        Assert.False(translator.IsAvailable);
    }

    [Fact]
    public void AddSelectionTranslationFeature_registers_both_local_and_cloud_translators()
    {
        var services = new ServiceCollection();

        services.AddSelectionTranslationFeature(EmptySettings);
        var provider = services.BuildServiceProvider();

        var translators = provider.GetServices<ITranslator>().ToList();

        Assert.Contains(translators, t => t.EngineName.Contains("ArgosLocalTranslator"));
        Assert.Contains(translators, t => t.EngineName == "AzureTranslator");
    }

    [Fact]
    public void AddSelectionTranslationFeature_falls_back_to_settings_api_key_when_env_var_not_set()
    {
        Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", null);
        var settings = new TranslationSettings(
            EnginePriorityOrder: Array.Empty<string>(),
            EngineEnabled: new Dictionary<string, bool>(),
            ApiKeys: new Dictionary<string, string> { ["Azure"] = "settings-key" });
        var services = new ServiceCollection();

        services.AddSelectionTranslationFeature(settings);
        var provider = services.BuildServiceProvider();

        var azure = provider.GetServices<ITranslator>().Single(t => t.EngineName == "AzureTranslator");
        Assert.True(azure.IsAvailable);
    }

    [Fact]
    public void AddSelectionTranslationFeature_env_var_takes_priority_over_settings_api_key()
    {
        Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", "env-key");
        try
        {
            var settings = new TranslationSettings(
                EnginePriorityOrder: Array.Empty<string>(),
                EngineEnabled: new Dictionary<string, bool>(),
                ApiKeys: new Dictionary<string, string> { ["Azure"] = "settings-key" });
            var services = new ServiceCollection();

            services.AddSelectionTranslationFeature(settings);
            var provider = services.BuildServiceProvider();

            var azure = provider.GetServices<ITranslator>().Single(t => t.EngineName == "AzureTranslator");
            Assert.True(azure.IsAvailable);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", null);
        }
    }

    [Fact]
    public async Task AddSelectionTranslationFeature_excludes_translator_disabled_in_settings_from_orchestrator()
    {
        Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", "env-key");
        try
        {
            var settings = new TranslationSettings(
                EnginePriorityOrder: Array.Empty<string>(),
                EngineEnabled: new Dictionary<string, bool> { ["AzureTranslator"] = false },
                ApiKeys: new Dictionary<string, string>());
            var services = new ServiceCollection();

            services.AddSelectionTranslationFeature(settings);
            var provider = services.BuildServiceProvider();
            var orchestrator = provider.GetRequiredService<ITranslationOrchestrator>();

            // AzureTranslatorはIsAvailable=trueだが設定で無効化されているため除外され、
            // ローカルNMTも未有効(環境変数未設定)のため利用可能なエンジンが無くなるはず
            await Assert.ThrowsAsync<NoTranslatorAvailableException>(
                () => orchestrator.TranslateAsync("hello", new LanguagePair("en", "ja"), CancellationToken.None));
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", null);
        }
    }
}
