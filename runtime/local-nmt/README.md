# 実験的ローカルNMT翻訳機能(Phase 4)

## ライセンスに関する重要な注意

**この機能で使用するArgos Translateのen→jaモデルは、商用利用ライセンスが確定していません。**

- `argosopentech/argos-translate` の GitHub Issue #507(2025-12-21作成、2026-07-17時点でopen・未回答)にて、`translate-en_ja-1_1.argosmodel` がREADMEにライセンス未記載のモデルの1つとして名指しされています。
- 開発者本人(Issue #76)も「モデルはコードと同じMIT/CC0のつもりだが、学習データの一部はライセンス不明であり、フェアユースだと思うが自分は弁護士ではない」と述べており、法的な保証はしていません。
- 別のコミュニティ監査でも、一部モデルが商用利用不可の学習データを含むと指摘されています。

**商用配布・商用利用の前には、必ず法務確認(開発者への直接確認、または別ライセンスモデルへの切り替え)を行ってください。** 詳細は `docs/prd.md` セクション6を参照。

## 有効化方法

この機能はデフォルトで無効です。有効化するには以下の環境変数を設定してください。

```
YAKUMADO_ENABLE_EXPERIMENTAL_LOCAL_NMT=1
YAKUMADO_LOCAL_NMT_PYTHON_PATH=<リポジトリroute>\runtime\local-nmt\.venv\Scripts\python.exe
YAKUMADO_LOCAL_NMT_SCRIPT_PATH=<リポジトリroot>\runtime\local-nmt\translate_server.py
```

3つとも設定されていない場合、`ArgosLocalTranslator.IsAvailable` は `false` を返し、クラウド翻訳エンジンへ自動的にフォールバックする(サーキットブレーカー的な安全側デフォルト)。

## セットアップ手順

```
cd runtime/local-nmt
uv venv --python 3.11
uv pip install -r requirements.txt
```

初回翻訳実行時、`translate_server.py` がArgos Translateのen→jaパッケージを自動ダウンロード・インストールします(インターネット接続が必要)。

## 動作確認済み事項(Phase 0 PoC②、`docs/poc-phase0-results.md`参照)

- 速度: 暖機後47〜86ms/文(CPU、GPUなし)
- 品質: 6文中4文が良好、1文に明確な誤訳、1文が直訳的
