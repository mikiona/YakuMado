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
using YakuMado.Settings;
using YakuMado.TextAcquisition;
using YakuMado.Translation.Orchestration;

var settingsFilePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "YakuMado", "settings.json");
Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);

// 翻訳エンジンの優先順位・有効/無効・APIキーの登録(DI合成)に必要なため、
// サービス構築より先に設定ファイルを読み込む。
var settings = await new DpapiTranslationSettingsRepository(settingsFilePath).LoadAsync(CancellationToken.None);

var services = new ServiceCollection();
services.AddYakuMadoCore(settingsFilePath);
services.AddSelectionTranslationFeature(settings);
services.AddOverlayTranslationFeature();
var provider = services.BuildServiceProvider();

var settingsRepository = provider.GetRequiredService<ITranslationSettingsRepository>();

Console.WriteLine("YakuMado: 選択テキスト翻訳 + 画面オーバーレイ翻訳");
Console.WriteLine($"設定ファイル: {settingsFilePath}");
Console.WriteLine($"登録済み翻訳エンジン数: {settings.EnginePriorityOrder.Count}");

var azureAvailable = provider.GetRequiredService<ITranslator>().IsAvailable;
Console.WriteLine($"AzureTranslator利用可否(環境変数または設定画面で保存したAPIキーの有無): {azureAvailable}");

if (!azureAvailable)
{
    Console.WriteLine("警告: AzureTranslatorのAPIキーが未設定のため、実際の翻訳は行えません。環境変数AZURE_TRANSLATOR_KEYを設定するか、トレイアイコンの「設定...」からAPIキーを保存してください。");
}
Console.WriteLine("ホットキー: Ctrl+Alt+T=選択テキスト翻訳 / Ctrl+Alt+O=画面オーバーレイ翻訳(トグル)。トレイアイコンから終了できます。");

// WPFのHwndSource/Window/トレイアイコンを扱うためSTAスレッドで実行する
var uiThread = new Thread(() => RunUi(provider, azureAvailable, settings, settingsRepository));
uiThread.SetApartmentState(ApartmentState.STA);
uiThread.Start();
uiThread.Join();

static void RunUi(
    IServiceProvider provider,
    bool azureAvailable,
    TranslationSettings currentSettings,
    ITranslationSettingsRepository settingsRepository)
{
    const uint MOD_CONTROL = 0x0002;
    const uint MOD_ALT = 0x0001;
    const uint VK_T = 0x54;
    const uint VK_O = 0x4F;

    var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

    var selectionHotkey = new GlobalHotkeyService(MOD_CONTROL | MOD_ALT, VK_T);
    var overlayHotkey = new GlobalHotkeyService(MOD_CONTROL | MOD_ALT, VK_O);
    var trayIcon = new TrayIconController();

    var acquisitionChain = provider.GetRequiredService<ISelectionAcquisitionChain>();
    var orchestrator = provider.GetRequiredService<ITranslationOrchestrator>();
    var popupController = provider.GetRequiredService<ISelectionPopupController>();

    var captureService = provider.GetRequiredService<IScreenCaptureService>();
    var ocrEngine = provider.GetRequiredService<IOcrEngine>();
    var overlayContentBuilder = provider.GetRequiredService<OcrOverlayContentBuilder>();
    var overlayWindowController = provider.GetRequiredService<IOverlayWindowController>();
    var isOverlayVisible = false;

    async void OnSelectionTranslateRequested()
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
    }

    async void OnOverlayTranslateToggleRequested()
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

        if (!ocrEngine.IsAvailable)
        {
            Console.WriteLine("英語のOCR言語パックがインストールされていないため、画面オーバーレイ翻訳は実行できません。");
            Console.WriteLine("Windowsの「設定 > 時刻と言語 > 言語と地域」から英語の言語パックを追加してください。");
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
    }

    selectionHotkey.SelectionTranslateRequested += (_, _) => OnSelectionTranslateRequested();
    overlayHotkey.SelectionTranslateRequested += (_, _) => OnOverlayTranslateToggleRequested();

    void OnSettingsRequested()
    {
        var translators = provider.GetServices<ITranslator>().ToList();
        var settingsViewModel = new SettingsViewModel(currentSettings, translators);
        var settingsWindow = new SettingsWindow(settingsViewModel, settingsRepository);
        settingsWindow.ShowDialog();
    }

    trayIcon.SelectionTranslateRequested += (_, _) => OnSelectionTranslateRequested();
    trayIcon.OverlayTranslateToggleRequested += (_, _) => OnOverlayTranslateToggleRequested();
    trayIcon.SettingsRequested += (_, _) => OnSettingsRequested();
    trayIcon.ExitRequested += (_, _) => app.Dispatcher.InvokeShutdown();

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
    trayIcon.Dispose();
}
