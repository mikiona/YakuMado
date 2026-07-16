using YakuMado.Core.Translation;

namespace YakuMado.Translation.Orchestration;

/// <summary>固定容量のLRU(最近最少使用)翻訳結果キャッシュ。スレッドセーフ。</summary>
public sealed class LruTranslationCache : ITranslationCache
{
    private readonly int _capacity;
    private readonly object _lock = new();
    private readonly Dictionary<(string Key, LanguagePair Pair), LinkedListNode<CacheEntry>> _map = new();
    private readonly LinkedList<CacheEntry> _lruOrder = new();

    public LruTranslationCache(int capacity = 500)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public bool TryGet(string normalizedKey, LanguagePair languagePair, out TranslationResult? result)
    {
        lock (_lock)
        {
            var mapKey = (normalizedKey, languagePair);
            if (_map.TryGetValue(mapKey, out var node))
            {
                _lruOrder.Remove(node);
                _lruOrder.AddFirst(node);
                result = node.Value.Result;
                return true;
            }

            result = null;
            return false;
        }
    }

    public void Set(string normalizedKey, LanguagePair languagePair, TranslationResult result)
    {
        lock (_lock)
        {
            var mapKey = (normalizedKey, languagePair);
            if (_map.TryGetValue(mapKey, out var existingNode))
            {
                _lruOrder.Remove(existingNode);
                _map.Remove(mapKey);
            }

            var node = new LinkedListNode<CacheEntry>(new CacheEntry(normalizedKey, languagePair, result));
            _lruOrder.AddFirst(node);
            _map[mapKey] = node;

            while (_map.Count > _capacity)
            {
                var last = _lruOrder.Last!;
                _lruOrder.RemoveLast();
                _map.Remove((last.Value.Key, last.Value.LanguagePair));
            }
        }
    }

    private sealed record CacheEntry(string Key, LanguagePair LanguagePair, TranslationResult Result);
}
