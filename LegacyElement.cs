using System.Xml.Linq;

namespace ProjectEditor;

/// <summary>
/// レガシー要素の情報を保持するクラス
/// </summary>
public class LegacyElement
{
    /// <summary>
    /// 要素名
    /// </summary>
    public string ElementName { get; set; } = string.Empty;

    /// <summary>
    /// 属性値または要素値
    /// </summary>
    public string AttributeValue { get; set; } = string.Empty;

    /// <summary>
    /// 削除推奨理由
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// XML要素への参照
    /// </summary>
    public XElement? Element { get; set; }
}
