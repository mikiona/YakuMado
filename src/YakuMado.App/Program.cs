using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YakuMado.App;
using YakuMado.Core.Input;
using YakuMado.Core.Ocr;
using YakuMado.Core.Overlay;
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
services.AddOverlayTranslationFeature();
var provider = services.BuildServiceProvider();

var settingsRepository = provider.GetRequiredService<ITranslationSettingsRepository>();
var settings = await settingsRepository.LoadAsync(CancellationToken.None);

Console.WriteLine("YakuMado Phase 3: 選択テキスト翻訳 + 画面オーバーレイ翻訳MVP");
Console.WriteLine($"設定ファイル: {settingsFilePath}");
Console.WriteLine($"登録済み翻訳エンジン数: {settings.EnginePriorityOrder.Count}");

var azureAvailable = provider.GetRequiredService<ITranslator>().IsAvailable;
Console.WriteLine($"AzureTranslator利用可否(AZURE_TRANSLATOR_KEY環境変数の有無): {azureAvailable}");

if (!azureAvailable)
{
    Console.WriteLine("警告: AZURE_TRANSLATOR_KEY環境変数が未設定のため、実際の翻訳は行えません。");
}
Console.WriteLine("ホットキー: Ctrl+Alt+T=選択テキスト翻訳 / Ctrl+Alt+O=画面オーバーレイ翻訳(トグル)。Ctrl+Cで終了。");

// WPFのHwndSource/Windowを扱うためSTAスレッドで実行する
var uiThread = new Thread(() => RunUi(provider, azureAvailable));
uiThread.SetApartmentState(ApartmentState.STA);
uiThread.Start();
uiThread.Join();

static void RunUi(IServiceProvider provider, bool azureAvailable)
{
    const uint MOD_CONTROL = 0x0002;
    const uint MOD_ALT = 0x0001;
    const uint VK_T = 0x54;
    const uint VK_O = 0x4F;

    var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

    var selectionHotkey = new GlobalHotkeyService(MOD_CONTROL | MOD_ALT, VK_T);
    var overlayHotkey = new GlobalHotkeyService(MOD_CONTROL | MOD_ALT, VK_O);

    var acquisitionChain = provider.GetRequiredService<ISelectionAcquisitionChain>();
    var orchestrator = provider.GetRequiredService<ITranslationOrchestrator>();
    var popupController = provider.GetRequiredService<ISelectionPopupController>();

    var captureService = provider.GetRequiredService<IScreenCaptureService>();
    var ocrEngine = provider.GetRequiredService<IOcrEngine>();
    var overlayContentBuilder = provider.GetRequiredService<OcrOverlayContentBuilder>();
    var overlayWindowController = provider.GetRequiredService<IOverlayWindowController>();
    var isOverlayVisible = false;

    selectionHotkey.SelectionTranslateRequested += async (_, _) =>
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
            popupController.ShowPopup("(翻訳エンジン未設定のためダミー表示) " + selectedText, new ScreenPoint(200, 200));
            return;
        }

        try
        {
            var result = await orchestrator.TranslateAsync(selectedText, new LanguagePair("en", "ja"), CancellationToken.None);
            popupController.ShowPopup(result.TranslatedText, new ScreenPoint(200, 200));
            Console.WriteLine($"翻訳結果: {result.TranslatedText} ({result.EngineName}, {result.Elapsed.TotalMilliseconds:F0}ms)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"翻訳に失敗しました: {ex.Message}");
        }
    };

    overlayHotkey.SelectionTranslateRequested += async (_, _) =>
    {
        if (isOverlayVisible)
        {
            overlayWindowController.ClearOverlay();
            isOverlayVisible = false;
            Console.WriteLine("画面オーバーレイを非表示にしました。");
            return;
        }

        if (!azureAvailable)
        {
            Console.WriteLine("AZURE_TRANSLATOR_KEY未設定のため画面オーバーレイ翻訳は実行できません。");
            return;
        }

        try
        {
            var region = new ScreenRect(
                0, 0,
                (int)SystemParameters.PrimaryScreenWidth,
                (int)SystemParameters.PrimaryScreenHeight);
            var bitmap = await captureService.CaptureAsync(region, CancellationToken.None);
            var ocrResult = await ocrEngine.RecognizeAsync(bitmap, CancellationToken.None);
            Console.WriteLine($"OCR検出行数: {ocrResult.Lines.Count}");

            var blocks = await overlayContentBuilder.BuildAsync(ocrResult, new LanguagePair("en", "ja"), CancellationToken.None);
            overlayWindowController.ShowOverlay(blocks);
            isOverlayVisible = true;
            Console.WriteLine($"画面オーバーレイ表示: {blocks.Count}件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"画面オーバーレイ翻訳に失敗しました: {ex.Message}");
        }
    };

    selectionHotkey.Register();
    overlayHotkey.Register();

    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        app.Dispatcher.InvokeShutdown();
    };

    app.Run();

    selectionHotkey.Unregister();
    overlayHotkey.Unregister();
}
