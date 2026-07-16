using Microsoft.Extensions.DependencyInjection;
using YakuMado.App;
using YakuMado.Core.Translation;

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
}
