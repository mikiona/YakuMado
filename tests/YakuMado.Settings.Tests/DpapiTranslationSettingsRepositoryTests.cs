using YakuMado.Core.Translation;
using YakuMado.Settings;

namespace YakuMado.Settings.Tests;

public class DpapiTranslationSettingsRepositoryTests : IDisposable
{
    private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), $"yakumado-settings-test-{Guid.NewGuid():N}.json");

    [Fact]
    public async Task SaveAsync_then_LoadAsync_returns_equivalent_settings()
    {
        var repository = new DpapiTranslationSettingsRepository(_tempFilePath);
        var original = new TranslationSettings(
            EnginePriorityOrder: new[] { "LocalNmt", "Azure", "Google" },
            EngineEnabled: new Dictionary<string, bool> { ["LocalNmt"] = true, ["Azure"] = true, ["Google"] = false },
            ApiKeys: new Dictionary<string, string> { ["Azure"] = "super-secret-api-key-12345" });

        await repository.SaveAsync(original, CancellationToken.None);
        var loaded = await repository.LoadAsync(CancellationToken.None);

        Assert.Equal(original.EnginePriorityOrder, loaded.EnginePriorityOrder);
        Assert.Equal(original.EngineEnabled, loaded.EngineEnabled);
        Assert.Equal(original.ApiKeys["Azure"], loaded.ApiKeys["Azure"]);
    }

    [Fact]
    public async Task SaveAsync_does_not_persist_api_key_in_plaintext_on_disk()
    {
        var repository = new DpapiTranslationSettingsRepository(_tempFilePath);
        var settings = new TranslationSettings(
            EnginePriorityOrder: new[] { "Azure" },
            EngineEnabled: new Dictionary<string, bool> { ["Azure"] = true },
            ApiKeys: new Dictionary<string, string> { ["Azure"] = "super-secret-api-key-12345" });

        await repository.SaveAsync(settings, CancellationToken.None);

        var rawFileContent = await File.ReadAllTextAsync(_tempFilePath);
        Assert.DoesNotContain("super-secret-api-key-12345", rawFileContent);
    }

    [Fact]
    public async Task LoadAsync_returns_empty_settings_when_file_does_not_exist()
    {
        var repository = new DpapiTranslationSettingsRepository(_tempFilePath);

        var loaded = await repository.LoadAsync(CancellationToken.None);

        Assert.Empty(loaded.EnginePriorityOrder);
        Assert.Empty(loaded.EngineEnabled);
        Assert.Empty(loaded.ApiKeys);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
    }
}
