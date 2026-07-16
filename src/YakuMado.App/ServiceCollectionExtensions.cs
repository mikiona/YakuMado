using Microsoft.Extensions.DependencyInjection;
using YakuMado.Core.TextAcquisition;
using YakuMado.Core.Translation;
using YakuMado.Overlay;
using YakuMado.Settings;
using YakuMado.TextAcquisition;
using YakuMado.Translation.Cloud;
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
    /// クラウドAPIキーはハードコードせず環境変数(AZURE_TRANSLATOR_KEY / AZURE_TRANSLATOR_REGION)から読み込む。</summary>
    public static IServiceCollection AddSelectionTranslationFeature(this IServiceCollection services)
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

        services.AddHttpClient();
        services.AddSingleton<ITranslator>(sp =>
        {
            var httpClient = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(string.Empty);
            var apiKey = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");
            var region = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION");
            return new AzureTranslator(httpClient, apiKey, region);
        });

        services.AddSingleton<ITranslationCache>(_ => new LruTranslationCache());
        services.AddSingleton<ITranslationOrchestrator>(sp => new TranslationOrchestrator(
            sp.GetServices<ITranslator>().ToList(),
            sp.GetRequiredService<ITranslationCache>()));

        services.AddSingleton<SelectionPopupViewModel>();

        return services;
    }
}
