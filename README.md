# VolatileMemo

本入力前の思考整理に使う、**保存しない"揮発メモ"**です。  
アプリを閉じる/隠すたびに内容を消去し、履歴や自動保存を行いません。

## 主な機能
- グローバルホットキーで表示（`Alt + \`、必要に応じて `Alt + Shift + \`）
- 表示位置はマウスカーソル付近（画面外にはみ出し防止）
- `Ctrl + Enter` でクリップボードへコピーして閉じる
- `Esc` で破棄して閉じる
- `×` / `Alt + F4` は終了せず、破棄して非表示
- システムトレイ常駐（開く / クリア / 終了）

## 使い方
1. アプリを起動します（通常はトレイ常駐）。
2. `Alt + \` でメモHUDを表示して入力します。
3. 終了時:
   - 内容を使う: `Ctrl + Enter`（コピーして閉じる）
   - 内容を捨てる: `Esc`（破棄して閉じる）

## 終了方法
- トレイアイコン右クリック → `終了`

## 開発環境
- .NET 10
- WPF（Windows）
- Visual Studio 2022 以降推奨

## ビルドと実行
```powershell
dotnet build .\VolatileMemoHUD.sln
dotnet run --project .\VolatileMemoHUD.csproj
```

## Publish（self-contained / single-file 例）
```powershell
dotnet publish .\VolatileMemoHUD.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

出力先例:
- `bin\Release\net10.0-windows\win-x64\publish\VolatileMemoHUD.exe`
