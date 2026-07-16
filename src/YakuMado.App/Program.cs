using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YakuMado.App;
using YakuMado.Core.Input;
using YakuMado.Core.TextAcquisition;
using YakuMado.Core.Translation;
using YakuMado.Overlay;
using YakuMado.TextAcquisition;
using YakuMado.Translation.Orchestration;

var settingsFilePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "YakuMado", "settings.json");
Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);

var services = new ServiceCollection();
services.AddYakuMadoCore(settingsFilePath);
services.AddSelectionTranslationFeature();
var provider = services.BuildServiceProvider();

var settingsRepository = provider.GetRequiredService<ITranslationSettingsRepository>();
var settings = await settingsRepository.LoadAsync(CancellationToken.None);

Console.WriteLine("YakuMado Phase 2: 選択テキスト翻訳MVP");
Console.WriteLine($"設定ファイル: {settingsFilePath}");
Console.WriteLine($"登録済み翻訳エンジン数: {settings.EnginePriorityOrder.Count}");

var azureAvailable = provider.GetRequiredService<ITranslator>().IsAvailable;
Console.WriteLine($"AzureTranslator利用可否(AZURE_TRANSLATOR_KEY環境変数の有無): {azureAvailable}");

if (!azureAvailable)
{
    Console.WriteLine("警告: AZURE_TRANSLATOR_KEY環境変数が未設定のため、実際の翻訳は行えません。");
    Console.WriteLine("ホットキー(Ctrl+Alt+T)の配線確認のみ行います。Ctrl+Cで終了してください。");
}

// Ctrl+Alt+T で選択テキスト翻訳を起動する(WPFのHwndSourceでメッセージのみのウィンドウを保持するためSTAスレッドで実行)
var uiThread = new Thread(() => RunUi(provider, azureAvailable));
uiThread.SetApartmentState(ApartmentState.STA);
uiThread.Start();
uiThread.Join();

static void RunUi(IServiceProvider provider, bool azureAvailable)
{
    const uint MOD_CONTROL = 0x0002;
    const uint MOD_ALT = 0x0001;
    const uint VK_T = 0x54;

    var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    var hotkeyService = new GlobalHotkeyService(MOD_CONTROL | MOD_ALT, VK_T);
    var acquisitionChain = provider.GetRequiredService<ISelectionAcquisitionChain>();
    var orchestrator = provider.GetRequiredService<ITranslationOrchestrator>();
    var popupViewModel = provider.GetRequiredService<SelectionPopupViewModel>();

    hotkeyService.SelectionTranslateRequested += async (_, _) =>
    {
        var selectedText = await acquisitionChain.AcquireAsync(CancellationToken.None);
        if (string.IsNullOrEmpty(selectedText))
        {
            Console.WriteLine("選択テキストを取得できませんでした。");
            return;
        }

        Console.WriteLine($"選択テキスト取得: {selectedText}");

        if (!azureAvailable)
        {
            popupViewModel.Show("(翻訳エンジン未設定のためダミー表示) " + selectedText);
            return;
        }

        try
        {
            var result = await orchestrator.TranslateAsync(selectedText, new LanguagePair("en", "ja"), CancellationToken.None);
            popupViewModel.Show(result.TranslatedText);
            Console.WriteLine($"翻訳結果: {result.TranslatedText} ({result.EngineName}, {result.Elapsed.TotalMilliseconds:F0}ms)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"翻訳に失敗しました: {ex.Message}");
        }
    };

    hotkeyService.Register();

    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        app.Dispatcher.InvokeShutdown();
    };

    app.Run();

    hotkeyService.Unregister();
}
