using System.Security.Cryptography;
using System.Text.Json;
using YakuMado.Core.Translation;

namespace YakuMado.Settings;

/// <summary>翻訳設定をJSONファイルに永続化する実装。APIキーはDPAPI(現在のユーザー単位)で暗号化してから保存する。</summary>
public sealed class DpapiTranslationSettingsRepository : ITranslationSettingsRepository
{
    private readonly string _filePath;

    public DpapiTranslationSettingsRepository(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<TranslationSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new TranslationSettings(
                EnginePriorityOrder: Array.Empty<string>(),
                EngineEnabled: new Dictionary<string, bool>(),
                ApiKeys: new Dictionary<string, string>());
        }

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        var stored = JsonSerializer.Deserialize<StoredSettings>(json)
            ?? throw new InvalidOperationException("設定ファイルの読み込みに失敗しました。");

        var decryptedApiKeys = stored.EncryptedApiKeys.ToDictionary(
            kv => kv.Key,
            kv => Decrypt(kv.Value));

        return new TranslationSettings(stored.EnginePriorityOrder, stored.EngineEnabled, decryptedApiKeys);
    }

    public async Task SaveAsync(TranslationSettings settings, CancellationToken cancellationToken)
    {
        var encryptedApiKeys = settings.ApiKeys.ToDictionary(kv => kv.Key, kv => Encrypt(kv.Value));

        var stored = new StoredSettings(
            settings.EnginePriorityOrder.ToArray(),
            new Dictionary<string, bool>(settings.EngineEnabled),
            encryptedApiKeys);

        var json = JsonSerializer.Serialize(stored);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);
    }

    private static string Encrypt(string plainText)
    {
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string Decrypt(string encryptedBase64)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedBase64);
        var plainBytes = ProtectedData.Unprotect(encryptedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }

    private sealed record StoredSettings(
        string[] EnginePriorityOrder,
        Dictionary<string, bool> EngineEnabled,
        Dictionary<string, string> EncryptedApiKeys);
}
