# Phase 4(ローカルNMT統合・実験的機能)実機検証メモ

## 背景

`docs/prd.md` セクション6で確認した通り、Argos Translateのen_jaモデルは商用利用ライセンスが未確定(GitHub Issue #507でREADME記載なしと確認済み、開発者本人も法的保証はしていない)。ユーザーとの相談の結果、「試験的に組み込む(ライセンス要確認のまま)」方針を採用し、既定で無効・明示的なオプトインでのみ有効化される実験的機能として実装した。

## 実装内容

- `YakuMado.Translation.Local/ArgosLocalTranslator.cs`: `ITranslator` の実装。Pythonサブプロセス(`runtime/local-nmt/translate_server.py`)とJSON Lines形式のstdin/stdout通信で連携する。
- `IsAvailable` は次の3条件をすべて満たす場合のみ `true`:
  1. 環境変数 `YAKUMADO_ENABLE_EXPERIMENTAL_LOCAL_NMT` が `1` または `true`
  2. `YAKUMADO_LOCAL_NMT_PYTHON_PATH` が指すファイルが存在する
  3. `YAKUMADO_LOCAL_NMT_SCRIPT_PATH` が指すファイルが存在する
- `runtime/local-nmt/README.md` にライセンス注意書きとセットアップ手順を明記。

## 実機検証結果

一時的な確認用プログラムで、既存のPhase 0 PoC②のPython venv(`poc/LocalNmtPoc/.venv`)を流用し、実際にC#からPythonサブプロセスを起動して翻訳を実行した(確認後にプログラムは削除済み)。

```
IsAvailable: True
[7276ms] 'Click the button below to continue.' -> '下のボタンをクリックして続行します。'
[56ms] 'An unexpected error occurred while processing your request.' -> 'リクエスト処理中に予期しないエラーが発生しました。'
```

- 初回呼び出し(モデルロード込み): 7276ms
- 2回目以降: 56ms(PoC②の47〜86ms/文という結果と整合)
- IsAvailable判定・JSON Lines通信・サブプロセス起動のいずれも正常に動作することを確認した。

## 既知の注意点

- サブプロセスの終了処理(`Dispose()`内の`Process.Kill(entireProcessTree: true)`)について、検証時に子プロセスが即座に終了せず残存する場面があった。venv経由のpython.exeが内部でランチャー・実体プロセスの2段構成になっている可能性があり、`entireProcessTree: true` を指定しているにも関わらずタイミングによっては残存しうる。本番実装では、プロセス終了後に一定時間待機してから残存確認を行う、またはジョブオブジェクト(Windows Job Object)による確実な子孫プロセス一括終了の導入を検討する余地がある。

## 結論

実験的ローカルNMT機能はコード・パイプラインとして実機で動作することを確認した。ただしライセンス問題は未解決のままであり、`runtime/local-nmt/README.md` の注意書きの通り、商用配布前には法務確認が必須である。
