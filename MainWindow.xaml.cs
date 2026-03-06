using System.Windows;
using System.IO;
using System.Windows.Input;
using System.Windows.Controls;

namespace ProjectEditor;

/// <summary>
/// プロジェクトファイル編集ツールのメインウィンドウ
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// MainWindowの新しいインスタンスを初期化します
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// ファイル追加ボタンのクリックイベントハンドラ
    /// </summary>
    private void OnAddFilesClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "プロジェクトファイル (*.csproj;*.vbproj)|*.csproj;*.vbproj|すべてのファイル (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                if (!FileListBox.Items.Contains(file))
                    FileListBox.Items.Add(file);
            }
        }
    }

    /// <summary>
    /// フォルダから追加ボタンのクリックイベントハンドラ
    /// </summary>
    private void OnAddFromFolderClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "プロジェクトファイルを検索するフォルダを選択"
        };

        if (dialog.ShowDialog() == true)
        {
            var folderPath = dialog.FolderName;
            var includeSubfolders = IncludeSubfoldersCheckBox.IsChecked == true;

            try
            {
                var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var csprojFiles = Directory.GetFiles(folderPath, "*.csproj", searchOption);
                var vbprojFiles = Directory.GetFiles(folderPath, "*.vbproj", searchOption);

                var allFiles = csprojFiles.Concat(vbprojFiles).ToArray();

                if (allFiles.Length == 0)
                {
                    MessageBox.Show("指定されたフォルダにプロジェクトファイルが見つかりませんでした。",
                        "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int addedCount = 0;
                foreach (var file in allFiles)
                {
                    if (!FileListBox.Items.Contains(file))
                    {
                        FileListBox.Items.Add(file);
                        addedCount++;
                    }
                }

                MessageBox.Show($"{addedCount} 個のプロジェクトファイルを追加しました。\n(合計: {allFiles.Length} 個のファイルが見つかりました)",
                    "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"フォルダの検索中にエラーが発生しました。{Environment.NewLine}{ex.Message}",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// クリアボタンのクリックイベントハンドラ
    /// </summary>
    private void OnClearClicked(object sender, RoutedEventArgs e)
    {
        FileListBox.Items.Clear();
    }

    /// <summary>
    /// レガシータグ削除ボタンのクリックイベントハンドラ
    /// .NET Framework時代の不要なタグを検出・削除します
    /// </summary>
    private void OnCleanupLegacyClicked(object sender, RoutedEventArgs e)
    {
        if (FileListBox.Items.Count == 0)
        {
            MessageBox.Show("ファイルが追加されていません。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 対象ファイルを決定
        var targetFiles = FileListBox.SelectedItems.Count > 0
            ? FileListBox.SelectedItems.Cast<string>()
            : FileListBox.Items.Cast<string>();

        // クリーンアップ実行
        LegacyCleanupHelper.ExecuteCleanup(targetFiles);
    }

    /// <summary>
    /// ListBoxアイテムの右クリックイベントハンドラ
    /// </summary>
    private void OnListBoxItemRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item)
        {
            item.IsSelected = true;
            item.Focus();
        }
    }

    /// <summary>
    /// 個別編集ボタンのクリックイベントハンドラ
    /// </summary>
    private void OnEditIndividualClicked(object sender, RoutedEventArgs e)
    {
        // 複数選択チェック
        if (FileListBox.SelectedItems.Count > 1)
        {
            MessageBox.Show("個別編集は1つのファイルのみ選択してください。\n複数のファイルが選択されています。", 
                "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (FileListBox.SelectedItem is not string selectedFile)
        {
            MessageBox.Show("編集するファイルを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var editorWindow = new PropertyEditorWindow(selectedFile)
        {
            Owner = this
        };

        editorWindow.ShowDialog();
    }

    /// <summary>
    /// リストから削除ボタンのクリックイベントハンドラ
    /// </summary>
    private void OnRemoveFromListClicked(object sender, RoutedEventArgs e)
    {
        var selectedItems = FileListBox.SelectedItems.Cast<object>().ToList();
        foreach (var item in selectedItems)
        {
            FileListBox.Items.Remove(item);
        }
    }

    /// <summary>
    /// 一括適用ボタンのクリックイベントハンドラ
    /// 選択されたファイル（または全ファイル）にプロパティを一括適用します
    /// </summary>
    private void OnApplyClicked(object sender, RoutedEventArgs e)
    {
        if (FileListBox.Items.Count == 0)
        {
            MessageBox.Show("ファイルが追加されていません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 対象ファイルを決定（選択されていればそれらのみ、なければ全て）
        var targetFiles = FileListBox.SelectedItems.Count > 0
            ? FileListBox.SelectedItems.Cast<string>().ToList()
            : FileListBox.Items.Cast<string>().ToList();

        // 共通プロパティ
        Dictionary<string, string> commonProperties = new();

        // 基本設定
        AddPropertyIfNotEmpty(commonProperties, "TargetFramework", TargetFrameworkInput.Text);
        AddPropertyIfNotEmpty(commonProperties, "LangVersion", LangVersionInput.Text);
        AddPropertyIfNotEmpty(commonProperties, "PlatformTarget", (PlatformTargetInput.SelectedItem as ComboBoxItem)?.Content?.ToString());

        // コード品質設定（共通）
        AddPropertyIfNotEmpty(commonProperties, "TreatWarningsAsErrors", (TreatWarningsAsErrorsInput.SelectedItem as ComboBoxItem)?.Content?.ToString());
        AddPropertyIfNotEmpty(commonProperties, "GenerateDocumentationFile", (GenerateDocumentationFileInput.SelectedItem as ComboBoxItem)?.Content?.ToString());

        // C#専用プロパティ
        Dictionary<string, string> csharpOnlyProperties = new();
        AddPropertyIfNotEmpty(csharpOnlyProperties, "Nullable", (NullableInput.SelectedItem as ComboBoxItem)?.Content?.ToString());
        AddPropertyIfNotEmpty(csharpOnlyProperties, "ImplicitUsings", (ImplicitUsingsInput.SelectedItem as ComboBoxItem)?.Content?.ToString());

        // プロジェクト設定
        AddPropertyIfNotEmpty(commonProperties, "RootNamespace", RootNamespaceInput.Text);
        AddPropertyIfNotEmpty(commonProperties, "AssemblyName", AssemblyNameInput.Text);
        AddPropertyIfNotEmpty(commonProperties, "AppendTargetFrameworkToOutputPath", (AppendTargetFrameworkToOutputPathInput.SelectedItem as ComboBoxItem)?.Content?.ToString());
        AddPropertyIfNotEmpty(commonProperties, "SatelliteResourceLanguages", SatelliteResourceLanguagesInput.Text);

        // バージョン情報
        AddPropertyIfNotEmpty(commonProperties, "AssemblyVersion", AssemblyVersionInput.Text);
        AddPropertyIfNotEmpty(commonProperties, "FileVersion", FileVersionInput.Text);

        // メタデータ
        AddPropertyIfNotEmpty(commonProperties, "Copyright", CopyrightInput.Text);
        AddPropertyIfNotEmpty(commonProperties, "Authors", AuthorsInput.Text);
        AddPropertyIfNotEmpty(commonProperties, "Company", CompanyInput.Text);
        AddPropertyIfNotEmpty(commonProperties, "Product", ProductInput.Text);
        AddPropertyIfNotEmpty(commonProperties, "Description", DescriptionInput.Text);

        // Debug構成のプロパティ
        Dictionary<string, string> debugProperties = new();
        var debugTypeDebugItem = DebugTypeDebugInput.SelectedItem as ComboBoxItem;
        var debugTypeDebug = debugTypeDebugItem?.Content?.ToString();
        var outputPathDebug = OutputPathDebugInput.Text;
        
        AddPropertyIfNotEmpty(debugProperties, "DebugType", debugTypeDebug);
        AddPropertyIfNotEmpty(debugProperties, "OutputPath", outputPathDebug);

        // Release構成のプロパティ
        Dictionary<string, string> releaseProperties = new();
        var debugTypeReleaseItem = DebugTypeReleaseInput.SelectedItem as ComboBoxItem;
        var debugTypeRelease = debugTypeReleaseItem?.Content?.ToString();
        var outputPathRelease = OutputPathReleaseInput.Text;
        
        AddPropertyIfNotEmpty(releaseProperties, "DebugType", debugTypeRelease);
        AddPropertyIfNotEmpty(releaseProperties, "OutputPath", outputPathRelease);

        if (commonProperties.Count == 0 && csharpOnlyProperties.Count == 0 && 
            debugProperties.Count == 0 && releaseProperties.Count == 0)
        {
            MessageBox.Show("設定する値を入力してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int successCount = 0;
        var errors = new List<string>();
        var skippedProperties = new List<string>();
        var nonAnyCpuWarnings = new List<string>();

        foreach (string file in targetFiles)
        {
            try
            {
                var modifier = new ProjectModifier(file);
                modifier.Load();

                // AnyCPU以外のConditionがあるかチェック
                if (modifier.HasNonAnyCpuConditions())
                {
                    var fileName = Path.GetFileName(file);
                    var conditions = modifier.GetAllConditions();
                    var nonStandardConditions = conditions.Where(c =>
                        c != "'$(Configuration)|$(Platform)'=='Debug|AnyCPU'" &&
                        c != "'$(Configuration)|$(Platform)'=='Release|AnyCPU'").ToList();

                    if (nonStandardConditions.Count > 0)
                    {
                        nonAnyCpuWarnings.Add($"{fileName}: AnyCPU以外のConditionが存在します");
                        foreach (var condition in nonStandardConditions)
                        {
                            nonAnyCpuWarnings.Add($"  - {condition}");
                        }
                    }
                }

                // 共通プロパティを適用
                modifier.SetProperties(commonProperties);

                // 言語固有プロパティを適用
                if (modifier.IsCSharpProject())
                {
                    modifier.SetProperties(csharpOnlyProperties);
                }
                else if (modifier.IsVBProject() && csharpOnlyProperties.Count > 0)
                {
                    // VB.NETプロジェクトにC#専用プロパティがある場合は記録
                    var fileName = Path.GetFileName(file);
                    skippedProperties.Add($"{fileName}: C#専用プロパティ（Nullable, ImplicitUsings）はスキップされました");
                }

                // Debug構成のプロパティを適用
                if (debugProperties.Count > 0)
                {
                    modifier.SetPropertiesWithCondition("'$(Configuration)|$(Platform)'=='Debug|AnyCPU'", debugProperties);
                }

                // Release構成のプロパティを適用
                if (releaseProperties.Count > 0)
                {
                    modifier.SetPropertiesWithCondition("'$(Configuration)|$(Platform)'=='Release|AnyCPU'", releaseProperties);
                }

                modifier.Save();
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        var appliedTo = FileListBox.SelectedItems.Count > 0 
            ? $"選択された {successCount} ファイル" 
            : $"{successCount} ファイル（全て）";
        var message = $"{appliedTo}に適用しました。";
        
        if (nonAnyCpuWarnings.Count > 0)
        {
            message += $"{Environment.NewLine}{Environment.NewLine}警告:{Environment.NewLine}{string.Join(Environment.NewLine, nonAnyCpuWarnings)}";
            message += $"{Environment.NewLine}※個別編集機能を使用して、これらのConditionを編集してください。";
        }
        if (skippedProperties.Count > 0)
        {
            message += $"{Environment.NewLine}{Environment.NewLine}注意:{Environment.NewLine}{string.Join(Environment.NewLine, skippedProperties)}";
        }
        if (errors.Count > 0)
        {
            message += $"{Environment.NewLine}{Environment.NewLine}エラー:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
        }

        MessageBox.Show(message, "完了", MessageBoxButton.OK,
            errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    /// <summary>
    /// プロパティが空でない場合にディクショナリに追加します
    /// </summary>
    /// <param name="properties">プロパティディクショナリ</param>
    /// <param name="key">プロパティキー</param>
    /// <param name="value">プロパティ値</param>
    private static void AddPropertyIfNotEmpty(Dictionary<string, string> properties, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            properties[key] = value;
        }
    }
}