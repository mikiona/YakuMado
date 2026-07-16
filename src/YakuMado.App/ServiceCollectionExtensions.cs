using Microsoft.Extensions.DependencyInjection;
using YakuMado.Core.Translation;
using YakuMado.Settings;

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
}
