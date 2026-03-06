# ProjectEditor

.NET プロジェクトファイル（.csproj / .vbproj）の一括編集ツール

## 概要

ProjectEditor は、複数の .NET プロジェクトファイルのプロパティを効率的に一括編集できる WPF アプリケーションです。
ソリューション内の多数のプロジェクトファイルを手動で編集する手間を省き、統一された設定を簡単に適用できます。

## 主な機能

### 📁 ファイル管理
- **個別ファイル追加**: .csproj / .vbproj ファイルを個別に追加
- **フォルダ一括追加**: フォルダ内のプロジェクトファイルを再帰的に検索して一括追加
- **サブフォルダ検索**: サブフォルダを含めた検索に対応

### ⚙️ 一括編集
- **基本設定**
  - TargetFramework (例: net8.0, net7.0)
  - LangVersion (例: 12.0, latest)
  - PlatformTarget (AnyCPU, x86, x64, ARM64)

- **コード品質**
  - Nullable (Null許容参照型) [C#専用]
  - ImplicitUsings (グローバルusing) [C#専用]
  - TreatWarningsAsErrors (警告をエラーとして扱う)
  - GenerateDocumentationFile (XMLドキュメント生成)

- **プロジェクト設定**
  - RootNamespace (デフォルトの名前空間)
  - AssemblyName (実行ファイル/DLL名)
  - AppendTargetFrameworkToOutputPath
  - SatelliteResourceLanguages

- **バージョン情報**
  - AssemblyVersion (厳密名バージョン)
  - FileVersion (ファイルバージョン)

- **メタデータ**
  - Copyright (著作権表示)
  - Authors (作成者名)
  - Company (会社/組織名)
  - Product (製品名)
  - Description (説明)

- **ビルド構成設定**
  - Debug/Release 構成ごとの DebugType 設定
  - Debug/Release 構成ごとの OutputPath 設定

### 🎯 柔軟な適用オプション
- **選択ファイルのみ適用**: リストから特定のファイルを選択して適用
- **全ファイル適用**: 未選択時は自動的にすべてのファイルに適用

### ✏️ 個別編集
- DataGrid による直感的な編集
- プリセットからプロパティを選択して追加
- Condition付きプロパティ（ビルド構成ごとの設定）にも対応

### 🔍 インテリジェント機能
- **言語判定**: C# / VB.NET プロジェクトを自動判定
- **C#専用プロパティ**: VB.NET プロジェクトには自動的にスキップ
- **AnyCPU以外の検出**: x86/x64などの特殊な構成を警告

### 🧹 レガシータグクリーンアップ
- **.NET Framework → .NET 8 移行支援**
- **古い要素の自動検出**: Reference要素、古いプロパティなどを検出
- **プレビュー表示**: 削除前に検出された要素を確認
- **一括削除**: 検出された古い要素を安全に削除

#### クリーンアップ対象の要素

**古いReference要素:**
- System, System.Core, System.Xml.Linq
- System.Data, System.Net.Http, System.Xml
- Microsoft.CSharp
- System.Drawing, System.Windows.Forms
- WPF関連 (WindowsBase, PresentationCore, PresentationFramework, System.Xaml)

**古いプロパティ:**
- TargetFrameworkVersion, TargetFrameworkProfile
- FileAlignment, AutoGenerateBindingRedirects
- ErrorReport, WarningLevel
- UseVSHostingProcess (VS2017以降廃止)
- Deterministic (.NET 8でデフォルトtrue)
- GenerateAssemblyInfo (.NET 8でデフォルトtrue)
- GenerateDocumentationFile=false (.NET 8でデフォルトfalse、明示不要)
- DocumentationFile (.NET 8ではGenerateDocumentationFileを使用)
- NoWarn=(空) (空の警告抑制は不要)
- ImportWindowsDesktopTargets (.NET 8ではUseWPF/UseWindowsFormsを使用)
- **ソース管理関連**: SccProjectName, SccLocalPath, SccAuxPath, SccProvider
- **その他**: OldToolsVersion, TargetFrameworkIdentifier, ProjectTypeGuids

**古いItemGroup要素:**
- **Compile Include**: SDK形式では*.csファイルは自動的に含まれる（特別なメタデータがない場合）
  - 保持される例: `<DependentUpon>`, `<AutoGen>`, `<DesignTime>`, `<SubType>`を持つもの
- **None Update**: 特別な設定がない場合は不要
  - 保持される例: `<CopyToOutputDirectory>`, `<Generator>`, `<LastGenOutput>`, `<DependentUpon>`, `<CopyToPublishDirectory>`を持つもの
- **None Include**: SDK形式では自動的に含まれる（特別なメタデータがない場合）
  - 保持される例: 上記と同じ特別なメタデータを持つもの

## システム要件

- **.NET 8.0 SDK** 以上
- **Windows 10/11** (WPFアプリケーション)

## ビルド方法

```bash
# リポジトリをクローン
git clone https://github.com/NYSTM/ProjectEditor.git
cd ProjectEditor

# ビルド
dotnet build

# 実行
dotnet run
```

## 使い方

### 1. ファイルの追加

**個別追加:**
```
「ファイルを追加」ボタン → .csproj / .vbproj ファイルを選択
```

**フォルダから一括追加:**
```
「フォルダから追加」ボタン → フォルダを選択 → サブフォルダを含めるかチェック
```

### 2. 一括編集設定

1. 編集したいプロパティの値を入力
2. 空欄のプロパティはスキップされます
3. 初期値が設定されている項目もあります

### 3. ファイルの選択（オプション）

- **特定のファイルのみ編集**: Ctrl/Shiftキーで複数選択
- **全ファイル編集**: 何も選択しない

### 4. 一括適用

「一括適用」ボタンをクリックして変更を保存

### 5. 個別編集（詳細編集が必要な場合）

1. 1つのファイルを選択
2. 「個別編集」ボタンまたは右クリックメニューから起動
3. DataGridで直接編集
4. プリセットから新しいプロパティを追加

### 6. レガシータグのクリーンアップ（.NET Framework移行時）

1. 移行したプロジェクトファイルを追加
2. 「🧹 レガシータグ削除」ボタンをクリック
3. 検出された古い要素をプレビュー
4. 「はい」を選択して削除実行

**⚠️ 重要**: クリーンアップ前に必ずバックアップを取ってください！

## 編集不可プロパティ

以下のプロパティは一括編集では変更できません（個別編集では可能）:
- **Version** (NuGetパッケージバージョン)
- **OutputType** (Exe/Library/WinExe)
- **RepositoryUrl** (リポジトリURL)

## 技術スタック

- **.NET 8.0**
- **WPF (Windows Presentation Foundation)**
- **C# 12.0**
- **XML処理**: System.Xml.Linq

## アーキテクチャ

```
ProjectEditor/
├── MainWindow.xaml(.cs)          # メインウィンドウ
├── PropertyEditorWindow.xaml(.cs) # 個別編集ウィンドウ
├── ProjectModifier.cs             # プロジェクトファイル操作クラス
├── App.xaml(.cs)                  # アプリケーションエントリポイント
├── LICENSE                        # MITライセンス
└── README.md                      # このファイル
```

### ProjectModifier クラス

プロジェクトファイルの読み込み、編集、保存を担当するコアクラス:

- `Load()`: プロジェクトファイルを読み込み
- `SetProperty()`: 通常のプロパティを設定
- `SetPropertyWithCondition()`: Condition付きプロパティを設定
- `GetAllProperties()`: すべてのプロパティを取得（Condition付きも含む）
- `DetectLegacyElements()`: 古い要素を検出
- `RemoveLegacyElements()`: 古い要素を削除
- `Save()`: 変更をファイルに保存

## 注意事項

⚠️ **バックアップ推奨**: 大量のプロジェクトファイルを編集する前に、必ずバージョン管理システム（Git等）でバックアップを取ってください。

⚠️ **元に戻す機能について**: 本ツールには専用の元に戻す機能はありません。変更前に必ずGit等のバージョン管理でバックアップまたはコミットを作成してください。

⚠️ **Condition付きプロパティ**: AnyCPU以外のプラットフォーム構成（x86/x64等）がある場合、個別編集機能を使用してください。

⚠️ **VB.NETプロジェクト**: C#専用プロパティ（Nullable, ImplicitUsings）は自動的にスキップされます。

⚠️ **レガシークリーンアップ**: .NET Framework → .NET 8 移行時は、レガシータグクリーンアップ機能を使用して不要な要素を削除できます。ただし、必ずバージョン管理でバックアップを取ってから実行してください。

## 免責事項

本ソフトウェアは「現状有姿」で提供されます。作者および貢献者は、本ソフトウェアの使用によって生じたいかなる損害（プロジェクトファイルの破損、データの損失、ビルドエラー、その他の問題を含むがこれらに限定されない）についても、一切の責任を負いません。

本ソフトウェアの使用は、すべて利用者自身の責任において行ってください。重要なプロジェクトに対して使用する場合は、必ず事前にバックアップを取得し、バージョン管理システム（Git等）を使用することを強く推奨します。

本ソフトウェアは、商用利用、非商用利用を問わず、無保証で提供されます。適合性、特定目的への適合性、第三者の権利の非侵害についての黙示の保証を含む、いかなる種類の保証もありません。

## ライセンス

このプロジェクトは MIT ライセンスの下で公開されています。詳細は [LICENSE](LICENSE) ファイルをご覧ください。

## 更新履歴

### v1.0.0 (2024-01-02)
- 初回リリース
- 基本的な一括編集機能
- 個別編集機能
- Condition付きPropertyGroup対応
- C# / VB.NET プロジェクト判定
- レガシータグクリーンアップ機能（.NET Framework移行支援）
