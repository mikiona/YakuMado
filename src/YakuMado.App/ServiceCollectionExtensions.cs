using Microsoft.Extensions.DependencyInjection;
using YakuMado.Core.Ocr;
using YakuMado.Core.Overlay;
using YakuMado.Core.TextAcquisition;
using YakuMado.Core.Translation;
using YakuMado.Ocr;
using YakuMado.Overlay;
using YakuMado.Settings;
using YakuMado.TextAcquisition;
using YakuMado.Translation.Cloud;
using YakuMado.Translation.Local;
using YakuMado.Translation.Orchestration;

namespace YakuMado.App;

/// <summary>DIコンテナへのYakuMado基盤サービス登録をまとめる合成ルート。</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYakuMadoCore(this IServiceCollection services, string settingsFilePath)
    {
        services.AddSingleton<ITranslationSettingsRepository>(
            _ => new DpapiTranslationSettingsRepository(settingsFilePath));

        return services;
    }

    /// <summary>選択テキスト翻訳MVP(Phase 2)に必要なサービスを登録する。
    /// クラウドAPIキーはハードコードせず、環境変数(AZURE_TRANSLATOR_KEY / AZURE_TRANSLATOR_REGION)を
    /// 優先し、未設定の場合は設定UIで保存された値(DPAPI暗号化・<paramref name="settings"/>)に
    /// フォールバックする。翻訳エンジンの優先順位・有効/無効も<paramref name="settings"/>に従う。</summary>
    public static IServiceCollection AddSelectionTranslationFeature(
        this IServiceCollection services, TranslationSettings settings)
    {
        services.AddSingleton<IClipboardAccessor, WpfClipboardAccessor>();
        services.AddSingleton<ICopyCommandSender, SendInputCopyCommandSender>();
        services.AddSingleton<UiAutomationSelectionProvider>();
        services.AddSingleton(sp => new ClipboardSelectionProvider(
            sp.GetRequiredService<IClipboardAccessor>(),
            sp.GetRequiredService<ICopyCommandSender>()));
        services.AddSingleton<ISelectionAcquisitionChain>(sp => new SelectionAcquisitionChain(new ITextSelectionProvider[]
        {
            sp.GetRequiredService<UiAutomationSelectionProvider>(),
            sp.GetRequiredService<ClipboardSelectionProvider>(),
        }));

        // 実験的ローカルNMT(Phase 4)。ライセンス未確定のため既定では無効(runtime/local-nmt/README.md参照)。
        // 優先順位はローカル→クラウドとし、ローカルが利用不可の場合はTranslationOrchestratorが自動でクラウドへフォールバックする。
        services.AddSingleton<ITranslator>(_ => new ArgosLocalTranslator(
            Environment.GetEnvironmentVariable("YAKUMADO_LOCAL_NMT_PYTHON_PATH") ?? string.Empty,
            Environment.GetEnvironmentVariable("YAKUMADO_LOCAL_NMT_SCRIPT_PATH") ?? string.Empty));

        services.AddHttpClient();
        services.AddSingleton<ITranslator>(sp =>
        {
            var httpClient = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(string.Empty);
            var apiKey = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY")
                ?? settings.ApiKeys.GetValueOrDefault("Azure");
            var region = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION")
                ?? settings.ApiKeys.GetValueOrDefault("AzureRegion");
            return new AzureTranslator(httpClient, apiKey, region);
        });

        services.AddSingleton<ITranslationCache>(_ => new LruTranslationCache());
        services.AddSingleton<ICircuitBreaker>(_ => new ConsecutiveFailureCircuitBreaker());
        services.AddSingleton<ITranslationOrchestrator>(sp => new TranslationOrchestrator(
            TranslatorPriorityFilter.Apply(sp.GetServices<ITranslator>().ToList(), settings),
            sp.GetRequiredService<ITranslationCache>(),
            sp.GetRequiredService<ICircuitBreaker>()));

        services.AddSingleton<SelectionPopupViewModel>();
        services.AddSingleton<ISelectionPopupController>(sp =>
            new WpfSelectionPopupController(sp.GetRequiredService<SelectionPopupViewModel>()));

        return services;
    }

    /// <summary>画面オーバーレイ翻訳MVP(Phase 3)に必要なサービスを登録する。</summary>
    public static IServiceCollection AddOverlayTranslationFeature(this IServiceCollection services)
    {
        services.AddSingleton<IScreenCaptureService, GdiScreenCaptureService>();
        services.AddSingleton<IFrameChangeDetector, PixelHashFrameChangeDetector>();
        services.AddSingleton<IOcrEngine>(_ => new WindowsOcrEngine("en"));
        services.AddSingleton(sp => new OcrOverlayContentBuilder(sp.GetRequiredService<ITranslationOrchestrator>()));
        services.AddSingleton<IOverlayWindowController, WpfOverlayWindowController>();

        return services;
    }
}
