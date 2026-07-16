# Phase 3(画面オーバーレイ翻訳MVP)実機検証メモ

## 検証内容

`YakuMado.Ocr` の `GdiScreenCaptureService`(画面キャプチャ)と `WindowsOcrEngine`(Windows.Media.Ocrラッパー)を、実機で一時的な確認用プログラムを作成し実行した(確認後に削除済み)。

## 確認できたこと(事実)

1. **画面キャプチャは正常に動作する**: `Graphics.CopyFromScreen`(BitBlt)で実際の画面内容(動画再生中の画面)を正しく取得できることを、保存したPNG画像を目視して確認した。今回のテスト環境では黒画面問題は発生しなかった。ただし、これは一例であり、tech-research-screen-translation.mdが指摘する「一部のハードウェアアクセラレーション使用アプリで黒画面になる場合がある」という制約が解消されたことを意味しない(別のGPU/描画方式では再現しうる)。
2. **OCRパイプライン自体は正常に動作する**: キャプチャ画像→`SoftwareBitmap`変換→`Windows.Media.Ocr.OcrEngine.RecognizeAsync`→行・単語のバウンディングボックス集計、という一連の処理が実際に動作し、画面上の英語テキスト("NVIDIA GTC Taipei 2026 Keynote | Full Replay")をテキストとして検出できた。

## 重要な発見(未解決の制約)

- **このマシンには日本語(ja)OCR言語パックしかインストールされておらず、英語(en)言語パックは未インストールだった**。`OcrEngine.TryCreateFromLanguage(new Language("en"))` は `null` を返し、`WindowsOcrEngine`は想定通り例外を送出した(異常終了ではなく、意図した挙動)。
- やむを得ず日本語パックでテストしたところ、OCR自体は動作したが英語テキストの認識精度が大きく低下した("NVIDIA GTC Taipei 2026 Keynote | Full Replay" → "NVlDlAGTCTaipei2026 Keynotel Full Replay"、スペル・スペースの誤認識あり)。これは言語パックの不一致によるものであり、正しい英語パックがあれば改善すると推測されるが未検証。

## PRDへの影響

英→日翻訳が主要ユースケースであるため、**エンドユーザーの環境にWindows OCRの英語言語パックがインストールされている必要がある**。これは新たに判明したシステム要件であり、`docs/prd.md` に追記する。対応案:
- アプリ起動時に `OcrEngine.AvailableRecognizerLanguages` を確認し、英語パックが無い場合はユーザーに「設定 > 時刻と言語 > 言語と地域」からのインストールを促す通知を表示する(Phase 6で実装検討)。

## 結論

Phase 3のOCR・キャプチャ実装は技術的に機能することを確認した。ただし英語OCR言語パックの有無という環境依存の制約が新たに判明したため、その旨をPRDに反映する。
