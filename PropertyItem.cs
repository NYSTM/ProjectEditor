namespace ProjectEditor;

/// <summary>
/// プロパティアイテムを表すクラス
/// </summary>
public class PropertyItem
{
    /// <summary>
    /// プロパティ名
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// プロパティ値
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
