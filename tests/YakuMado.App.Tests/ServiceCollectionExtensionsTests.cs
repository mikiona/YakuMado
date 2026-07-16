using Microsoft.Extensions.DependencyInjection;
using YakuMado.App;
using YakuMado.Core.TextAcquisition;
using YakuMado.Core.Translation;
using YakuMado.Overlay;
using YakuMado.Translation.Orchestration;

namespace YakuMado.App.Tests;

public class ServiceCollectionExtensionsTests
{
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

        services.AddSelectionTranslationFeature();
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

        services.AddSelectionTranslationFeature();
        var provider = services.BuildServiceProvider();

        var translator = provider.GetRequiredService<ITranslator>();
        Assert.False(translator.IsAvailable);
    }
}
