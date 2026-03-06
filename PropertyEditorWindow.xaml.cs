using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ProjectEditor;

/// <summary>
/// 個別のプロジェクトファイルのプロパティを編集するウィンドウ
/// </summary>
public partial class PropertyEditorWindow : Window
{
    /// <summary>
    /// 編集対象のファイルパス
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// プロパティのコレクション
    /// </summary>
    public ObservableCollection<PropertyItem> Properties { get; set; } = new();

    /// <summary>
    /// PropertyEditorWindowの新しいインスタンスを初期化します
    /// </summary>
    /// <param name="filePath">編集対象のファイルパス</param>
    public PropertyEditorWindow(string filePath)
    {
        InitializeComponent();
        FilePath = filePath;
        DataContext = this;
        LoadProperties();
    }

    /// <summary>
    /// プロジェクトファイルからプロパティを読み込みます
    /// </summary>
    private void LoadProperties()
    {
        try
        {
            var modifier = new ProjectModifier(FilePath);
            modifier.Load();
            var properties = modifier.GetAllProperties();

            Properties.Clear();
            foreach (var kvp in properties)
            {
                Properties.Add(new PropertyItem { Key = kvp.Key, Value = kvp.Value });
            }

            PropertyGrid.ItemsSource = Properties;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"プロパティの読み込みに失敗しました。{Environment.NewLine}{ex.Message}", 
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    /// <summary>
    /// プロパティ追加ボタンのクリックイベントハンドラ
    /// </summary>
    private void OnAddPropertyClicked(object sender, RoutedEventArgs e)
    {
        var selectedItem = NewPropertyComboBox.SelectedItem as ComboBoxItem;
        var propertyName = selectedItem?.Content?.ToString();

        if (string.IsNullOrWhiteSpace(propertyName) || propertyName == "-- Select Property --")
        {
            MessageBox.Show("プロパティを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 既に存在するプロパティかチェック
        if (Properties.Any(p => p.Key == propertyName))
        {
            MessageBox.Show($"プロパティ '{propertyName}' は既に存在します。", "警告",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 新しいプロパティを追加
        var initialValue = NewPropertyValueTextBox.Text ?? string.Empty;
        Properties.Add(new PropertyItem { Key = propertyName, Value = initialValue });

        // リセット
        NewPropertyComboBox.SelectedIndex = 0;
        NewPropertyValueTextBox.Text = string.Empty;

        MessageBox.Show($"プロパティ '{propertyName}' を追加しました。", "成功",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 行削除ボタンのクリックイベントハンドラ
    /// </summary>
    private void OnDeleteRowClicked(object sender, RoutedEventArgs e)
    {
        if (PropertyGrid.SelectedItem is PropertyItem item)
        {
            Properties.Remove(item);
        }
    }

    /// <summary>
    /// 適用ボタンのクリックイベントハンドラ
    /// 変更をプロジェクトファイルに保存します
    /// </summary>
    private void OnApplyClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var modifier = new ProjectModifier(FilePath);
            modifier.Load();

            // すべての既存プロパティを削除してから再設定
            var existingProps = modifier.GetAllProperties();
            foreach (var key in existingProps.Keys)
            {
                modifier.RemoveProperty(key);
            }

            // 新しいプロパティを設定
            Dictionary<string, string> newProperties = new();
            foreach (var item in Properties)
            {
                if (!string.IsNullOrWhiteSpace(item.Key))
                {
                    newProperties[item.Key] = item.Value ?? string.Empty;
                }
            }

            modifier.SetProperties(newProperties);
            modifier.Save();

            MessageBox.Show("プロパティを保存しました。", "成功", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存に失敗しました。{Environment.NewLine}{ex.Message}", 
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// キャンセルボタンのクリックイベントハンドラ
    /// </summary>
    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
