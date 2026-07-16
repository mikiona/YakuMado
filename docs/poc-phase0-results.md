# Phase 0 PoC 実施結果

`docs/architecture.md` セクション6で定義したPhase 0(PoC①②③)を実機(このセッションが動作しているWindows環境)で実施した結果。ソースコードは `poc/` 配下に保存。

## PoC① 選択テキスト取得の実機検証

**方法**: 新規メモ帳ウィンドウを起動し、テスト文をValuePatternで設定→Ctrl+Aで全選択→以下2方式で取得を試行。

| 方式 | 結果 |
|------|------|
| UI Automation `TextPattern.GetSelection()` | **成功**: 選択テキストを正確に取得 |
| クリップボード方式(Ctrl+C→読取→復元) | **成功**: 選択テキストを正確に取得、元のクリップボード内容も復元できた |

**結論**: 両方式とも実機で機能することを確認。`docs/architecture.md` の3段階フォールバック設計(UI Automation→クリップボード→OCR)の土台となる最初の2段階は実装可能と判断。

コード: `poc/SelectionAcquisitionPoc/Program.cs`

## PoC③ オーバーレイ描画のクリックスルー実機検証

**方法**: WPFウィンドウに `WindowStyle=None` + `AllowsTransparency=True` を設定し、`SourceInitialized` 時に拡張ウィンドウスタイル `WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE` を付与。`GetWindowLong` での読み戻しで適用を確認した上で、`WindowFromPoint` によるOSレベルのヒットテストで検証。

| 検証項目 | 結果 |
|---------|------|
| 拡張ウィンドウスタイルの適用 | **確認**: `WS_EX_LAYERED=True`, `WS_EX_TRANSPARENT=True`, `WS_EX_NOACTIVATE=True` を`GetWindowLong`で読み戻し確認 |
| クリックスルー(`WindowFromPoint`によるヒットテスト) | **成功**: `WS_EX_TRANSPARENT`指定時はヒットテストがオーバーレイ自身を素通りし、実際に背後にあるウィンドウを返した。対照実験として`WS_EX_TRANSPARENT`を一時的に外すと、ヒットテストがオーバーレイ自身を返すことも確認(手法の妥当性を裏付け) |

**注記(実施環境固有の制約)**: このセッションの実行環境(バックグラウンドプロセスからのデスクトップ操作)では、`SetForegroundWindow`や合成マウスクリックによるウィンドウのフォアグラウンド化・アクティブ化が一貫して機能せず、検証対象として起動したメモ帳ウィンドウを実際に最前面へ引き上げることができなかった(既存の別ウィンドウがz-order上で最前面に残り続けた)。そのため「クリックがメモ帳自身に届く」ところまでは実証できていない。ただし`WindowFromPoint`によるヒットテストの対照実験により、**クリックスルー機構(WS_EX_TRANSPARENT)自体がOSレベルで正しく機能していること**は確認できている。実際のアプリ画面に対する最終確認は、人手による対話的操作(実際にブラウザ等の上にオーバーレイを表示しクリックする)で別途行うことを推奨する。

コード: `poc/OverlayClickThroughPoc/Program.cs`

## PoC② 英→日ローカルNMTモデルの実用性検証(最優先項目)

**背景**: `docs/tech-research-screen-translation.md` の追加検証で、Sugoi-v4は日→英専用(英→日に不適合)、Bergamotの高速化数値はen-pt限定でen-ja未確認と判明しており、英→日ローカルNMTモデルの実用可否が未解決の重要課題だった。

**方法**: Argos Translate(CTranslate2ベース、リポジトリ本体はMIT/CC0デュアルライセンス)経由でen→jaパッケージを導入し、代表的な英文6件を翻訳して品質(目視)と速度を計測。

**速度計測結果**(CPU、GPUなし)

| 文 | 翻訳時間 |
|----|---------|
| (初回、モデルロード込み) | 1851.9ms |
| 以降5文 | 47.0〜85.7ms |

初回のモデルロードコストを除けば、**1文あたり47〜86ms**で完了しており、PRDの非機能要件(選択テキスト翻訳300ms以内、ストレッチ100ms以内)を大きく上回る速度が出ている。

**品質評価結果(目視、6文中)**

| 原文 | 訳文 | 評価 |
|------|------|------|
| Click the button below to continue. | 下のボタンをクリックして続行します。 | ○ 自然 |
| Your session has expired. Please sign in again. | セッションが終了しました。 お問い合わせ | ✗ 誤訳(「Please sign in again.」が「お問い合わせ」という無関係な訳に) |
| This document contains confidential information. | この文書には機密情報が含まれています。 | ○ 正確 |
| The quick brown fox jumps over the lazy dog. | クイックブラウンのフォックスは、怠惰な犬の上にジャンプします。 | △ 直訳的だが意味は通じる |
| Settings > Privacy > Clipboard access | 設定 > プライバシー > クリップボードアクセス | ○ UI文言として適切 |
| An unexpected error occurred while processing your request. | リクエスト処理中に予期しないエラーが発生しました。 | ○ 自然 |

6文中4文が良好、1文が直訳的、1文に明確な誤訳(意味の欠落)。

**結論**: 実用に耐えうる速度と、大筋で許容範囲の翻訳品質を持つ英→日ローカルNMTモデルが**技術的には存在する**ことを確認した。ただし以下は未解決:

- **ライセンス未確認(重要)**: Argos Translate本体はMIT/CC0だが、配布されている各言語ペアの翻訳モデルデータ自体のライセンスはリポジトリREADME・パッケージインデックスページのいずれにも明記が見つからなかった。商用利用可否は別途確認が必要(GitHub Issuesでの問い合わせ等)。これが確認できるまで、本モデルを製品に組み込む判断は保留する。
- 誤訳が一定数(6件中1件)発生しており、単独の翻訳エンジンとして無条件に信頼するのはリスクがある。`docs/architecture.md` のサーキットブレーカー設計や、クラウドとの併用(ハイブリッド構成)による品質担保の重要性を裏付ける結果。

**PRDへの反映**: `docs/prd.md` セクション6「未解決の重要課題」を更新し、「モデルの技術的実用性は確認できたが、ライセンスが未確認」という状態に更新する。

コード: `poc/LocalNmtPoc/poc_en_ja.py`, `poc/LocalNmtPoc/requirements.txt`

## Phase 0 総合結論

| PoC | 判定 |
|-----|------|
| ① 選択テキスト取得 | 成功 |
| ② 英→日ローカルNMT | 技術的実用性は確認、ライセンスが未解決 |
| ③ オーバーレイクリックスルー | 機構自体は成功、実アプリでの最終確認は人手推奨 |

Phase 1(基盤構築)へ進める状態と判断する。ただしPhase 4(ローカルNMT統合)着手前に、Argos Translateモデルのライセンス確認を優先タスクとして行う必要がある。
