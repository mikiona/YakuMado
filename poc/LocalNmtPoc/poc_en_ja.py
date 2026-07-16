# PoC2: 英->日ローカルNMTモデルの実用性検証
# argostranslate(CTranslate2ベース、MITライセンス)経由でen->jaパッケージを導入し、
# 実際の翻訳品質(目視)と速度を計測する。
import time

import argostranslate.package
import argostranslate.translate

print("=== PoC2: 英->日ローカルNMT 実用性検証 ===")

print("[1] 利用可能なパッケージ一覧を更新中...")
argostranslate.package.update_package_index()
available_packages = argostranslate.package.get_available_packages()

en_ja_packages = [
    p for p in available_packages if p.from_code == "en" and p.to_code == "ja"
]
print(f"en->ja パッケージ候補: {[str(p) for p in en_ja_packages]}")

if not en_ja_packages:
    print("結果: en->jaパッケージが見つかりませんでした(Argos Translateのインデックスに存在しない)")
    raise SystemExit(1)

package_to_install = en_ja_packages[0]
print(f"[2] インストール対象: {package_to_install}")
download_path = package_to_install.download()
argostranslate.package.install_from_path(download_path)

print("[3] 翻訳テスト実行")
test_sentences = [
    "Click the button below to continue.",
    "Your session has expired. Please sign in again.",
    "This document contains confidential information.",
    "The quick brown fox jumps over the lazy dog.",
    "Settings > Privacy > Clipboard access",
    "An unexpected error occurred while processing your request.",
]

installed_languages = argostranslate.translate.get_installed_languages()
from_lang = next((lang for lang in installed_languages if lang.code == "en"), None)
to_lang = next((lang for lang in installed_languages if lang.code == "ja"), None)

if from_lang is None or to_lang is None:
    print(f"結果: 言語モデルのロードに失敗 (from_lang={from_lang}, to_lang={to_lang})")
    raise SystemExit(1)

translation = from_lang.get_translation(to_lang)

results = []
for sentence in test_sentences:
    start = time.perf_counter()
    translated = translation.translate(sentence)
    elapsed_ms = (time.perf_counter() - start) * 1000
    results.append((sentence, translated, elapsed_ms))
    print(f"  [{elapsed_ms:6.1f}ms] {sentence!r} -> {translated!r}")

avg_ms = sum(r[2] for r in results) / len(results)
print(f"\n平均翻訳時間(初回ロード後、CPU): {avg_ms:.1f}ms / 文")
print("=== PoC2 完了(品質は目視で確認してください) ===")
