using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectEditor;

public class ProjectModifier
{
    public string FilePath { get; private set; }
    private XDocument? _document;

    public ProjectModifier(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// プロジェクトがC#プロジェクトかどうかを判定します。
    /// </summary>
    public bool IsCSharpProject()
    {
        return Path.GetExtension(FilePath).Equals(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// プロジェクトがVB.NETプロジェクトかどうかを判定します。
    /// </summary>
    public bool IsVBProject()
    {
        return Path.GetExtension(FilePath).Equals(".vbproj", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// プロジェクトファイルを読み込みます。
    /// </summary>
    public void Load()
    {
        using var stream = File.OpenRead(FilePath);
        _document = XDocument.Load(stream);
    }

    /// <summary>
    /// プロパティに値を設定します。存在しない場合は追加します。
    /// </summary>
    public void SetProperty(string propertyName, string value)
    {
        if (_document?.Root == null)
            throw new InvalidOperationException("Load() が呼び出されていません。");

        if (string.IsNullOrWhiteSpace(value)) return;

        // 既存の PropertyGroup から検索
        var propertyGroups = _document.Root.Elements().Where(e => e.Name.LocalName == "PropertyGroup");
        foreach (var group in propertyGroups)
        {
            var element = group.Elements().FirstOrDefault(e => e.Name.LocalName == propertyName);
            if (element != null)
            {
                element.Value = value;
                return;
            }
        }

        // プロパティが未存在なら最初の PropertyGroup に追加
        var firstGroup = propertyGroups.FirstOrDefault();
        if (firstGroup == null)
        {
            // PropertyGroupを新規作成する際も名前空間を考慮
            var ns = _document.Root.Name.Namespace;
            firstGroup = new XElement(ns + "PropertyGroup");
            _document.Root.AddFirst(firstGroup);
        }

        // 新しいプロパティ要素を追加する際も名前空間を考慮
        var ns2 = firstGroup.Name.Namespace;
        firstGroup.Add(new XElement(ns2 + propertyName, value));
    }

    /// <summary>
    /// すべてのPropertyGroupからプロパティを取得します。
    /// Condition付きPropertyGroupのプロパティも含めます。
    /// </summary>
    public Dictionary<string, string> GetAllProperties()
    {
        if (_document?.Root == null)
            throw new InvalidOperationException("Load() を呼び出してください。");

        var properties = new Dictionary<string, string>();
        var propertyGroups = _document.Root.Elements().Where(e => e.Name.LocalName == "PropertyGroup");

        foreach (var group in propertyGroups)
        {
            var condition = group.Attribute("Condition")?.Value;
            
            foreach (var element in group.Elements())
            {
                string key;
                if (!string.IsNullOrWhiteSpace(condition))
                {
                    // Condition付きの場合はキーにConditionを含める
                    key = $"{element.Name.LocalName} [{condition}]";
                }
                else
                {
                    // 通常のプロパティ
                    key = element.Name.LocalName;
                }

                // 最初に見つかった値を優先
                if (!properties.ContainsKey(key))
                {
                    properties[key] = element.Value;
                }
            }
        }

        return properties;
    }

    /// <summary>
    /// 複数のプロパティを一括設定します。
    /// Condition付きプロパティ（キーに"[Condition]"を含む）にも対応します。
    /// </summary>
    public void SetProperties(Dictionary<string, string> properties)
    {
        foreach (var kvp in properties)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value))
            {
                // キーにConditionが含まれているかチェック
                if (kvp.Key.Contains('[') && kvp.Key.Contains(']'))
                {
                    // "PropertyName [Condition]" の形式を分解
                    var lastBracketIndex = kvp.Key.LastIndexOf('[');
                    var propertyName = kvp.Key[..lastBracketIndex].Trim();
                    var condition = kvp.Key[(lastBracketIndex + 1)..^1]; // [Condition] から Condition を抽出

                    SetPropertyWithCondition(condition, propertyName, kvp.Value);
                }
                else
                {
                    // 通常のプロパティ
                    SetProperty(kvp.Key, kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Condition付きPropertyGroupにプロパティを設定します。
    /// </summary>
    public void SetPropertyWithCondition(string condition, string propertyName, string value)
    {
        if (_document?.Root == null)
            throw new InvalidOperationException("Load() を呼び出してください。");

        if (string.IsNullOrWhiteSpace(value)) return;

        var propertyGroups = _document.Root.Elements().Where(e => e.Name.LocalName == "PropertyGroup");
        var targetGroup = propertyGroups.FirstOrDefault(g => 
            g.Attribute("Condition")?.Value == condition);

        if (targetGroup == null)
        {
            // 指定されたConditionのPropertyGroupが存在しない場合は作成
            var ns = _document.Root.Name.Namespace;
            targetGroup = new XElement(ns + "PropertyGroup", new XAttribute("Condition", condition));
            _document.Root.Add(targetGroup);
        }

        // 既存のプロパティを検索
        var element = targetGroup.Elements().FirstOrDefault(e => e.Name.LocalName == propertyName);
        if (element != null)
        {
            element.Value = value;
        }
        else
        {
            var ns = targetGroup.Name.Namespace;
            targetGroup.Add(new XElement(ns + propertyName, value));
        }
    }

    /// <summary>
    /// Condition付きPropertyGroupから複数のプロパティを設定します。
    /// </summary>
    public void SetPropertiesWithCondition(string condition, Dictionary<string, string> properties)
    {
        foreach (var kvp in properties)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value))
            {
                SetPropertyWithCondition(condition, kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// プロジェクトに存在するすべてのCondition付きPropertyGroupのCondition値を取得します。
    /// </summary>
    public List<string> GetAllConditions()
    {
        if (_document?.Root == null)
            throw new InvalidOperationException("Load() を呼び出してください。");

        var conditions = new List<string>();
        var propertyGroups = _document.Root.Elements().Where(e => e.Name.LocalName == "PropertyGroup");

        foreach (var group in propertyGroups)
        {
            var condition = group.Attribute("Condition")?.Value;
            if (!string.IsNullOrWhiteSpace(condition))
            {
                conditions.Add(condition);
            }
        }

        return conditions;
    }

    /// <summary>
    /// Condition文字列を正規化します（スペースを削除して比較可能な形式にします）。
    /// </summary>
    private static string NormalizeCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return string.Empty;

        // すべてのスペースを削除して正規化
        return Regex.Replace(condition, @"\s+", "");
    }

    /// <summary>
    /// Debug|AnyCPU または Release|AnyCPU 以外のConditionがあるかチェックします。
    /// </summary>
    public bool HasNonAnyCpuConditions()
    {
        var conditions = GetAllConditions();
        var standardConditions = new[]
        {
            "'$(Configuration)|$(Platform)'=='Debug|AnyCPU'",
            "'$(Configuration)|$(Platform)'=='Release|AnyCPU'"
        };

        // 標準Conditionも正規化
        var normalizedStandardConditions = standardConditions.Select(NormalizeCondition).ToHashSet();

        return conditions.Any(c => !normalizedStandardConditions.Contains(NormalizeCondition(c)));
    }

    /// <summary>
    /// プロパティを削除します。
    /// Condition付きプロパティ（キーに"[Condition]"を含む）にも対応します。
    /// </summary>
    public void RemoveProperty(string propertyName)
    {
        if (_document?.Root == null)
            throw new InvalidOperationException("Load() を呼び出してください。");

        // キーにConditionが含まれているかチェック
        if (propertyName.Contains('[') && propertyName.Contains(']'))
        {
            // "PropertyName [Condition]" の形式を分解
            var lastBracketIndex = propertyName.LastIndexOf('[');
            var actualPropertyName = propertyName[..lastBracketIndex].Trim();
            var condition = propertyName[(lastBracketIndex + 1)..^1];

            // 指定されたConditionのPropertyGroupから削除
            var propertyGroups = _document.Root.Elements().Where(e => e.Name.LocalName == "PropertyGroup");
            var targetGroup = propertyGroups.FirstOrDefault(g =>
                g.Attribute("Condition")?.Value == condition);

            if (targetGroup != null)
            {
                var element = targetGroup.Elements().FirstOrDefault(e => e.Name.LocalName == actualPropertyName);
                element?.Remove();
            }
        }
        else
        {
            // 通常のプロパティ（Condition無し）を削除
            var propertyGroups = _document.Root.Elements().Where(e => e.Name.LocalName == "PropertyGroup");
            foreach (var group in propertyGroups)
            {
                if (group.Attribute("Condition") == null)
                {
                    var element = group.Elements().FirstOrDefault(e => e.Name.LocalName == propertyName);
                    element?.Remove();
                }
            }
        }
    }

    /// <summary>
    /// ファイルに保存します。テンポラリファイル経由で書き込み、成功後にリネームします。
    /// </summary>
    public void Save()
    {
        if (_document == null)
            throw new InvalidOperationException("Load() が呼び出されていません。");

        var dir = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("ファイルパスからディレクトリを取得できませんでした。");
        var tempPath = Path.Combine(dir, Path.GetRandomFileName());

        try
        {
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                _document.Save(stream);
            }
            File.Replace(tempPath, FilePath, null);
        }
        catch
        {
            // 書き込み失敗時はテンポラリファイルを削除
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    /// <summary>
    /// .NET Framework時代の古い要素を検出します。
    /// </summary>
    /// <returns>検出された古い要素のリスト</returns>
    public List<LegacyElement> DetectLegacyElements()
    {
        if (_document == null)
            throw new InvalidOperationException("Load() が呼び出されていません。");

        var detector = new LegacyElementDetector(_document);
        return detector.DetectLegacyElements();
    }

    /// <summary>
    /// 検出された古い要素を削除します。
    /// </summary>
    /// <param name="elementsToRemove">削除する要素のリスト</param>
    /// <returns>削除された要素の数</returns>
    public int RemoveLegacyElements(List<LegacyElement> elementsToRemove)
    {
        if (_document == null)
            throw new InvalidOperationException("Load() が呼び出されていません。");

        var detector = new LegacyElementDetector(_document);
        return detector.RemoveLegacyElements(elementsToRemove);
    }

    /// <summary>
    /// クリーンアップレポートを生成します。
    /// </summary>
    /// <param name="legacyElements">検出された古い要素</param>
    /// <returns>レポート文字列</returns>
    public static string GetCleanupReport(List<LegacyElement> legacyElements)
    {
        return LegacyElementDetector.GetCleanupReport(legacyElements);
    }
}
