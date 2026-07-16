# YakuMado 実験的ローカルNMT翻訳サーバー(Phase 4)
#
# 【重要】Argos Translateのen->jaモデルは商用利用ライセンスが未確定(GitHub Issue #507で
# README記載なしと確認済み、開発者本人も法的保証はしていない)。本機能はYAKUMADO_ENABLE_
# EXPERIMENTAL_LOCAL_NMT環境変数による明示的なオプトインでのみ有効化される実験的機能であり、
# 商用配布前には必ずライセンスの法務確認を行うこと。
#
# 標準入力からJSON Lines形式(1行1リクエスト)でテキストを受け取り、翻訳結果を標準出力へ
# JSON Lines形式で返す常駐プロセス。C#側からProcessで起動し、stdin/stdoutで通信する。
import json
import sys
import time

import argostranslate.package
import argostranslate.translate


def ensure_package_installed(from_code: str, to_code: str) -> None:
    installed_languages = argostranslate.translate.get_installed_languages()
    if any(lang.code == from_code for lang in installed_languages) and \
       any(lang.code == to_code for lang in installed_languages):
        from_lang = next(lang for lang in installed_languages if lang.code == from_code)
        to_lang = next(lang for lang in installed_languages if lang.code == to_code)
        if from_lang.get_translation(to_lang) is not None:
            return

    argostranslate.package.update_package_index()
    available_packages = argostranslate.package.get_available_packages()
    matching = [p for p in available_packages if p.from_code == from_code and p.to_code == to_code]
    if not matching:
        raise RuntimeError(f"{from_code}->{to_code} のパッケージが見つかりません")
    download_path = matching[0].download()
    argostranslate.package.install_from_path(download_path)


def main() -> None:
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue

        try:
            request = json.loads(line)
            text = request["text"]
            from_code = request.get("from", "en")
            to_code = request.get("to", "ja")

            ensure_package_installed(from_code, to_code)

            installed_languages = argostranslate.translate.get_installed_languages()
            from_lang = next(lang for lang in installed_languages if lang.code == from_code)
            to_lang = next(lang for lang in installed_languages if lang.code == to_code)
            translation = from_lang.get_translation(to_lang)

            start = time.perf_counter()
            translated_text = translation.translate(text)
            elapsed_ms = (time.perf_counter() - start) * 1000

            response = {"translatedText": translated_text, "elapsedMs": elapsed_ms}
        except Exception as ex:  # noqa: BLE001 サーバープロセスを落とさないため意図的に全例外を捕捉
            response = {"error": f"{type(ex).__name__}: {ex}"}

        print(json.dumps(response, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
