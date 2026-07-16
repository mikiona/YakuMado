using Microsoft.Extensions.DependencyInjection;
using YakuMado.App;
using YakuMado.Core.Translation;

var settingsFilePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "YakuMado", "settings.json");
Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);

var services = new ServiceCollection();
services.AddYakuMadoCore(settingsFilePath);
var provider = services.BuildServiceProvider();

var settingsRepository = provider.GetRequiredService<ITranslationSettingsRepository>();
var settings = await settingsRepository.LoadAsync(CancellationToken.None);

Console.WriteLine("YakuMado 基盤(Phase 1)起動確認");
Console.WriteLine($"設定ファイル: {settingsFilePath}");
Console.WriteLine($"登録済み翻訳エンジン数: {settings.EnginePriorityOrder.Count}");
