# Phase 6 配布パッケージング

## 設定内容

`src/YakuMado.App/YakuMado.App.csproj` に自己完結型・単一実行ファイル配布のための設定を追加した。

```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

また `app.manifest` を追加し、Per-Monitor DPI Awareness V2を明示指定した(画面キャプチャ・オーバーレイの座標計算が実ピクセルと一致するために重要)。

## ビルドコマンド

```
dotnet publish src/YakuMado.App/YakuMado.App.csproj -c Release -r win-x64 --self-contained true -o publish
```

## 実機検証結果

- `publish/YakuMado.App.exe`(単一ファイル、約74.7MB、.NETランタイム込みの自己完結型)が生成されることを確認した。
- 生成された実行ファイルを直接起動し、クラッシュなく動作することを確認した(コンソール出力の日本語が文字化けしたが、これはこのBash実行環境のコンソールコードページに起因する表示上の問題であり、実際のWindows環境(通常のコマンドプロンプト/PowerShell)での動作には影響しないと考えられる。未確認)。

## スコープ外(今回未対応)

- MSI/MSIXなどのインストーラ作成は別途大きな作業になるため、本フェーズでは対応していない。現状は単一実行ファイルを配布し、ユーザーが任意の場所に配置して実行する形を想定する。
- スタートアップ登録(Windows起動時の自動起動)は未実装。
- コード署名は未実施(未署名の実行ファイルとしてWindows SmartScreenの警告が出る可能性がある、未検証)。
