# YakuMado

Windows用の翻訳ソフト。Webブラウザや各種アプリの画面上に表示された英語テキストを日本語に翻訳する。

- **選択テキスト翻訳**: ホットキーで選択中のテキストを翻訳しポップアップ表示
- **画面オーバーレイ翻訳**: 画面上の英語テキストをOCRで検出し、日本語訳をオーバーレイ表示
- 翻訳エンジンはクラウドAPI(Azure Translator)とローカルNMT(実験的機能)のハイブリッド構成

設計の背景・技術調査の詳細は [`docs/`](docs/) 配下のドキュメントを参照(下記「ドキュメント一覧」参照)。

## 必要環境

- Windows 10 (build 17134 / version 1803) 以降
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- 画面オーバーレイ翻訳(F2)を使う場合: Windows OCRの英語(en)言語パック
  (「設定 > 時刻と言語 > 言語と地域」からインストール。未インストールの場合、起動時にコンソールへ警告が表示される)
- クラウド翻訳を使う場合: Azure Translatorのリソース(APIキー・リージョン)
- 実験的ローカルNMTを使う場合(任意): [uv](https://docs.astral.sh/uv/)、Python 3.11

## ビルド方法

リポジトリ直下で以下を実行する。

```
dotnet build YakuMado.slnx
```

個別プロジェクトのみビルドする場合は該当の `.csproj` を指定する。

```
dotnet build src/YakuMado.App/YakuMado.App.csproj
```

## テスト実行方法

全プロジェクトのテストをまとめて実行する。

```
dotnet test YakuMado.slnx
```

個別プロジェクトのみ実行する場合。

```
dotnet test tests/YakuMado.Translation.Orchestration.Tests/YakuMado.Translation.Orchestration.Tests.csproj
```

## 実行方法

### 開発時(dotnet run)

```
dotnet run --project src/YakuMado.App/YakuMado.App.csproj
```

クラウド翻訳を有効にする場合は事前に環境変数を設定する(PowerShellの例)。

```powershell
$env:AZURE_TRANSLATOR_KEY = "<Azure Translatorのサブスクリプションキー>"
$env:AZURE_TRANSLATOR_REGION = "<リソースのリージョン(例: japaneast)>"
dotnet run --project src/YakuMado.App/YakuMado.App.csproj
```

環境変数が未設定の場合、選択テキスト翻訳は「翻訳エンジン未設定」を示すダミー表示に、画面オーバーレイ翻訳は実行不可になる(コンソールにその旨が表示される)。

### 配布用ビルド(自己完結型・単一実行ファイル)

```
dotnet publish src/YakuMado.App/YakuMado.App.csproj -c Release -r win-x64 --self-contained true -o publish
```

`publish/YakuMado.App.exe`(約75MB、.NETランタイム込み)が生成される。詳細は [`docs/phase6-packaging.md`](docs/phase6-packaging.md) を参照。MSI等のインストーラは未対応。

## アプリの使い方

起動すると常駐アプリとしてタスクトレイにアイコンが表示される(コンソールウィンドウにもログが出力される)。

### ホットキー

| ホットキー | 動作 |
|---|---|
| `Ctrl+Alt+T` | 選択中のテキストを翻訳してポップアップ表示 |
| `Ctrl+Alt+O` | 画面オーバーレイ翻訳の表示/非表示を切り替え |

### タスクトレイメニュー(右クリック)

- **選択テキスト翻訳** — `Ctrl+Alt+T` と同じ動作
- **画面オーバーレイ翻訳** — `Ctrl+Alt+O` と同じ動作
- **設定...** — 翻訳エンジンの優先順位・有効/無効、Azure APIキー・リージョンの設定(保存後は反映のためアプリの再起動が必要)
- **終了** — アプリを終了する

### 選択テキスト翻訳の使い方

1. 任意のアプリ(ブラウザ、メモ帳等)で翻訳したい英語テキストを選択する
2. `Ctrl+Alt+T` を押す(内部でUI Automation → クリップボード方式の順にテキスト取得を試みる)
3. 翻訳結果がポップアップウィンドウに表示される

### 画面オーバーレイ翻訳の使い方

1. 翻訳したい英語テキストが表示されている画面状態にする
2. `Ctrl+Alt+O` を押す(画面全体をキャプチャしOCR→翻訳→オーバーレイ表示)
3. もう一度 `Ctrl+Alt+O` を押すとオーバーレイを非表示にできる

## 環境変数一覧

| 環境変数 | 用途 |
|---|---|
| `AZURE_TRANSLATOR_KEY` | Azure Translatorのサブスクリプションキー(クラウド翻訳を有効化) |
| `AZURE_TRANSLATOR_REGION` | Azure Translatorのリソースリージョン(例: `japaneast`) |
| `YAKUMADO_ENABLE_EXPERIMENTAL_LOCAL_NMT` | `1` または `true` で実験的ローカルNMTを有効化(既定は無効) |
| `YAKUMADO_LOCAL_NMT_PYTHON_PATH` | ローカルNMT用Python実行ファイルのパス(`runtime/local-nmt/.venv/Scripts/python.exe`) |
| `YAKUMADO_LOCAL_NMT_SCRIPT_PATH` | ローカルNMTサーバースクリプトのパス(`runtime/local-nmt/translate_server.py`) |

実験的ローカルNMTのセットアップ手順・ライセンス注意事項は [`runtime/local-nmt/README.md`](runtime/local-nmt/README.md) を参照。

## プロジェクト構成

```
src/
  YakuMado.Core/                    Core層インターフェース(外部依存なし)
  YakuMado.Settings/                DPAPIによる設定永続化
  YakuMado.TextAcquisition/         選択テキスト取得(クリップボード/UI Automation/ホットキー)
  YakuMado.Ocr/                     OCR・画面キャプチャ
  YakuMado.Translation.Cloud/       Azure Translator
  YakuMado.Translation.Local/       実験的ローカルNMT(Argos Translate)
  YakuMado.Translation.Orchestration/ 翻訳エンジンのハイブリッド切替・キャッシュ・サーキットブレーカー
  YakuMado.Overlay/                 選択テキストポップアップ・画面オーバーレイ表示
  YakuMado.App/                    エントリポイント(DIコンテナ配線・ホットキー・トレイアイコン・設定UI)
tests/                              各srcプロジェクトに対応するテストプロジェクト
runtime/local-nmt/                  実験的ローカルNMT用のPythonサーバースクリプト
docs/                               技術調査・PRD・アーキテクチャ設計・実機検証メモ
```

## ドキュメント一覧

| ドキュメント | 内容 |
|---|---|
| [`docs/tech-research-screen-translation.md`](docs/tech-research-screen-translation.md) | 実現方法の技術調査レポート |
| [`docs/prd.md`](docs/prd.md) | 要件定義(機能要件・非機能要件・未解決課題) |
| [`docs/architecture.md`](docs/architecture.md) | アーキテクチャ設計 |
| [`docs/poc-phase0-results.md`](docs/poc-phase0-results.md) | Phase 0 PoC実機検証結果 |
| [`docs/phase3-verification-notes.md`](docs/phase3-verification-notes.md) | 画面オーバーレイ翻訳の実機検証メモ(OCR言語パック等) |
| [`docs/phase4-verification-notes.md`](docs/phase4-verification-notes.md) | ローカルNMT統合の実機検証メモ |
| [`docs/phase6-perf-benchmark.md`](docs/phase6-perf-benchmark.md) | 性能ベンチマーク結果 |
| [`docs/phase6-packaging.md`](docs/phase6-packaging.md) | 配布パッケージングの詳細 |

## 既知の制約

- **実験的ローカルNMT(Argos Translate)の商用ライセンスは未確定** — 既定では無効。詳細は [`runtime/local-nmt/README.md`](runtime/local-nmt/README.md) と `docs/prd.md` セクション6を参照。
- **画面オーバーレイ翻訳には英語OCR言語パックが必要** — 未インストール環境では動作しない(検知・案内メッセージを実装済み)。
- **画面キャプチャはGDI(BitBlt)による暫定実装** — 一部のハードウェアアクセラレーション使用アプリで黒画面になる可能性がある(`Windows.Graphics.Capture`への置き換えは今後の課題)。
- インストーラ(MSI/MSIX)・スタートアップ自動起動・コード署名は未対応。
