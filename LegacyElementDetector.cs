using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ProjectEditor;

/// <summary>
/// .NET Framework 時代のレガシー要素の検出・削除・レポート生成を担当するクラス。
/// <see cref="ProjectModifier"/> から委譲される形で使用されます。
/// </summary>
public class LegacyElementDetector
{
    private readonly XDocument _document;

    /// <summary>
    /// <see cref="LegacyElementDetector"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="document">操作対象のプロジェクトファイルを表す <see cref="XDocument"/>。</param>
    public LegacyElementDetector(XDocument document)
    {
        _document = document;
    }

    /// <summary>
    /// .NET Framework 時代の古い要素をドキュメント全体から検出します。
    /// Reference 要素・プロパティ・Compile Include・None 要素の順に検査します。
    /// </summary>
    /// <returns>
    /// 検出された <see cref="LegacyElement"/> のリスト。
    /// ルート要素が存在しない場合は空のリストを返します。
    /// </returns>
    public List<LegacyElement> DetectLegacyElements()
    {
        if (_document.Root == null)
            return [];

        var legacyElements = new List<LegacyElement>();

        DetectLegacyReferences(legacyElements);
        DetectLegacyProperties(legacyElements);
        DetectLegacyCompileIncludes(legacyElements);
        DetectLegacyNoneItems(legacyElements);

        return legacyElements;
    }

    /// <summary>
    /// 指定されたレガシー要素を XML ドキュメントから削除します。
    /// <see cref="LegacyElement.Element"/> が <see langword="null"/> の項目はスキップします。
    /// </summary>
    /// <param name="elementsToRemove">削除対象の <see cref="LegacyElement"/> のリスト。</param>
    /// <returns>実際に削除された要素の件数。</returns>
    public int RemoveLegacyElements(List<LegacyElement> elementsToRemove)
    {
        int removedCount = 0;

        foreach (var legacy in elementsToRemove)
        {
            // XElement への参照が残っている要素のみ削除する
            if (legacy.Element != null)
            {
                legacy.Element.Remove();
                removedCount++;
            }
        }

        return removedCount;
    }

    /// <summary>
    /// 検出されたレガシー要素の一覧をテキスト形式のレポートとして生成します。
    /// 要素は種類（<see cref="LegacyElement.ElementName"/>）ごとにグループ化して出力します。
    /// </summary>
    /// <param name="legacyElements">検出済みの <see cref="LegacyElement"/> のリスト。</param>
    /// <returns>
    /// 書式化されたレポート文字列。
    /// 要素が存在しない場合はその旨を示す固定メッセージを返します。
    /// </returns>
    public static string GetCleanupReport(List<LegacyElement> legacyElements)
    {
        if (legacyElements.Count == 0)
            return "古い要素は検出されませんでした。";

        var report = new System.Text.StringBuilder();
        report.AppendLine($"検出された古い要素: {legacyElements.Count}個");
        report.AppendLine();

        // 要素種類ごとにグループ化して出力する
        var groupedByType = legacyElements.GroupBy(e => e.ElementName);
        foreach (var group in groupedByType)
        {
            report.AppendLine($"【{group.Key}】 ({group.Count()}個)");
            foreach (var element in group)
            {
                report.AppendLine($"  ・{element.AttributeValue}");
                report.AppendLine($"    理由: {element.Reason}");
            }
            report.AppendLine();
        }

        return report.ToString();
    }

    /// <summary>
    /// .NET 8 では暗黙的に参照される古い <c>Reference</c> 要素を検出します。
    /// Include 属性のアセンブリ簡易名（カンマより前の部分）を既知のリストと照合します。
    /// </summary>
    /// <param name="legacyElements">検出結果を追加するリスト。</param>
    private void DetectLegacyReferences(List<LegacyElement> legacyElements)
    {
        var references = _document.Root!.Descendants("Reference").ToList();

        // .NET 8 で暗黙的に参照されるため明示的な Reference 要素が不要なアセンブリ名一覧
        var legacyReferences = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "System", "System.Core", "System.Xml.Linq", "System.Data.DataSetExtensions",
            "Microsoft.CSharp", "System.Data", "System.Net.Http", "System.Xml",
            "System.Drawing", "System.Windows.Forms", "WindowsBase", "PresentationCore",
            "PresentationFramework", "System.Xaml"
        };

        // Include 属性値からアセンブリ簡易名（カンマ・空白より前の部分）を取り出す
        static string GetSimpleName(string include)
        {
            var comma = include.IndexOf(',');
            return (comma >= 0 ? include[..comma] : include).Trim();
        }

        foreach (var reference in references)
        {
            var includeName = reference.Attribute("Include")?.Value;
            if (includeName != null && legacyReferences.Contains(GetSimpleName(includeName)))
            {
                legacyElements.Add(new LegacyElement
                {
                    ElementName = "Reference",
                    AttributeValue = includeName,
                    Reason = ".NET 8では暗黙的に参照されるため不要",
                    Element = reference
                });
            }
        }
    }

    /// <summary>
    /// .NET 8 移行後に不要となった <c>PropertyGroup</c> 内のプロパティ要素を検出します。
    /// 固定の既知プロパティ名リストに加え、値に依存する条件付き検出も行います。
    /// </summary>
    /// <param name="legacyElements">検出結果を追加するリスト。</param>
    private void DetectLegacyProperties(List<LegacyElement> legacyElements)
    {
        var propertyGroups = _document.Root!.Elements("PropertyGroup");

        // .NET 8 では不要または代替プロパティへの移行が必要なプロパティ名と理由の対応表
        var legacyProperties = new Dictionary<string, string>
        {
            { "TargetFrameworkVersion", ".NET 8ではTargetFrameworkを使用" },
            { "TargetFrameworkProfile", ".NET 8では不要" },
            { "FileAlignment", ".NET 8では不要" },
            { "AutoGenerateBindingRedirects", ".NET 8では不要" },
            { "ErrorReport", ".NET 8では不要" },
            { "WarningLevel", ".NET 8ではデフォルト値が最適" },
            { "UseVSHostingProcess", "Visual Studio 2017以降では廃止" },
            { "Deterministic", ".NET 8ではデフォルトでtrue" },
            { "SccProjectName", "ソース管理の古い設定（不要）" },
            { "SccLocalPath", "ソース管理の古い設定（不要）" },
            { "SccAuxPath", "ソース管理の古い設定（不要）" },
            { "SccProvider", "ソース管理の古い設定（不要）" },
            { "OldToolsVersion", "古いツールバージョン設定（不要）" },
            { "TargetFrameworkIdentifier", ".NET 8では不要" },
            { "ProjectTypeGuids", "古いプロジェクトタイプ識別子（不要）" },
            { "GenerateAssemblyInfo", ".NET 8ではデフォルトでtrue（明示不要）" },
            { "ImportWindowsDesktopTargets", ".NET 8では不要（UseWPF/UseWindowsFormsを使用）" },
            { "DocumentationFile", ".NET 8ではGenerateDocumentationFileを使用" }
        };

        foreach (var group in propertyGroups)
        {
            // 既知プロパティ名リストと照合して検出する
            foreach (var property in legacyProperties)
            {
                var element = group.Element(property.Key);
                if (element != null)
                {
                    legacyElements.Add(new LegacyElement
                    {
                        ElementName = property.Key,
                        AttributeValue = element.Value,
                        Reason = property.Value,
                        Element = element
                    });
                }
            }

            // 値に依存する条件付きプロパティを検出する
            DetectConditionalLegacyProperties(group, legacyElements);
        }
    }

    /// <summary>
    /// 値の内容によって不要と判断されるプロパティ要素を検出します。
    /// <c>GenerateDocumentationFile=false</c> および値が空の <c>NoWarn</c> が対象です。
    /// </summary>
    /// <param name="group">検査対象の <c>PropertyGroup</c> 要素。</param>
    /// <param name="legacyElements">検出結果を追加するリスト。</param>
    private static void DetectConditionalLegacyProperties(XElement group, List<LegacyElement> legacyElements)
    {
        // GenerateDocumentationFile=false は .NET 8 のデフォルトと同じため明示不要
        var generateDocElement = group.Element("GenerateDocumentationFile");
        if (generateDocElement != null &&
            generateDocElement.Value.Equals("false", System.StringComparison.OrdinalIgnoreCase))
        {
            legacyElements.Add(new LegacyElement
            {
                ElementName = "GenerateDocumentationFile",
                AttributeValue = generateDocElement.Value,
                Reason = ".NET 8ではデフォルトでfalse（明示不要）",
                Element = generateDocElement
            });
        }

        // 値が空の NoWarn は警告を抑制する効果がなく不要
        var noWarnElement = group.Element("NoWarn");
        if (noWarnElement != null && string.IsNullOrWhiteSpace(noWarnElement.Value))
        {
            legacyElements.Add(new LegacyElement
            {
                ElementName = "NoWarn",
                AttributeValue = "(空)",
                Reason = "空のNoWarnは不要",
                Element = noWarnElement
            });
        }
    }

    /// <summary>
    /// SDK 形式のプロジェクトでは自動インクルードされる <c>Compile Include</c> 要素を検出します。
    /// <c>DependentUpon</c> などの特別なメタデータを持つ要素は保持対象として除外します。
    /// </summary>
    /// <param name="legacyElements">検出結果を追加するリスト。</param>
    private void DetectLegacyCompileIncludes(List<LegacyElement> legacyElements)
    {
        var compileIncludes = _document.Root!.Descendants("Compile")
            .Where(c => c.Attribute("Include") != null)
            .ToList();

        foreach (var compile in compileIncludes)
        {
            var includePath = compile.Attribute("Include")?.Value;
            if (includePath != null && includePath.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
            {
                // 特別なメタデータがない場合のみレガシーとして検出する
                if (!HasSpecialCompileMetadata(compile))
                {
                    legacyElements.Add(new LegacyElement
                    {
                        ElementName = "Compile Include",
                        AttributeValue = includePath,
                        Reason = "SDK形式では*.csファイルは自動的に含まれるため不要",
                        Element = compile
                    });
                }
            }
        }
    }

    /// <summary>
    /// SDK 形式のプロジェクトでは自動インクルードされる <c>None Update</c> および
    /// <c>None Include</c> 要素を検出します。
    /// <c>CopyToOutputDirectory</c> などの特別なメタデータを持つ要素は保持対象として除外します。
    /// </summary>
    /// <param name="legacyElements">検出結果を追加するリスト。</param>
    private void DetectLegacyNoneItems(List<LegacyElement> legacyElements)
    {
        // None Update 要素を検出する
        var noneUpdates = _document.Root!.Descendants("None")
            .Where(n => n.Attribute("Update") != null)
            .ToList();

        foreach (var none in noneUpdates)
        {
            var updatePath = none.Attribute("Update")?.Value;
            if (!HasSpecialNoneMetadata(none))
            {
                legacyElements.Add(new LegacyElement
                {
                    ElementName = "None Update",
                    AttributeValue = updatePath ?? "(不明)",
                    Reason = "特別な設定がないため不要",
                    Element = none
                });
            }
        }

        // None Include 要素を検出する
        var noneIncludes = _document.Root!.Descendants("None")
            .Where(n => n.Attribute("Include") != null)
            .ToList();

        foreach (var none in noneIncludes)
        {
            var includePath = none.Attribute("Include")?.Value;
            if (!HasSpecialNoneMetadata(none))
            {
                legacyElements.Add(new LegacyElement
                {
                    ElementName = "None Include",
                    AttributeValue = includePath ?? "(不明)",
                    Reason = "SDK形式では自動的に含まれるため不要",
                    Element = none
                });
            }
        }
    }

    /// <summary>
    /// <c>Compile</c> 要素が保持すべき特別なメタデータを持つかどうかを判定します。
    /// </summary>
    /// <param name="compile">検査対象の <c>Compile</c> 要素。</param>
    /// <returns>
    /// <c>DependentUpon</c>・<c>AutoGen</c>・<c>DesignTime</c>・<c>SubType</c>
    /// のいずれかを子要素に持つ場合は <see langword="true"/>。
    /// </returns>
    private static bool HasSpecialCompileMetadata(XElement compile)
    {
        return compile.Elements().Any(e =>
            e.Name.LocalName == "DependentUpon" ||
            e.Name.LocalName == "AutoGen" ||
            e.Name.LocalName == "DesignTime" ||
            e.Name.LocalName == "SubType");
    }

    /// <summary>
    /// <c>None</c> 要素が保持すべき特別なメタデータを持つかどうかを判定します。
    /// </summary>
    /// <param name="none">検査対象の <c>None</c> 要素。</param>
    /// <returns>
    /// <c>CopyToOutputDirectory</c>・<c>Generator</c>・<c>LastGenOutput</c>・
    /// <c>DependentUpon</c>・<c>CopyToPublishDirectory</c>
    /// のいずれかを子要素に持つ場合は <see langword="true"/>。
    /// </returns>
    private static bool HasSpecialNoneMetadata(XElement none)
    {
        return none.Elements().Any(e =>
            e.Name.LocalName == "CopyToOutputDirectory" ||
            e.Name.LocalName == "Generator" ||
            e.Name.LocalName == "LastGenOutput" ||
            e.Name.LocalName == "DependentUpon" ||
            e.Name.LocalName == "CopyToPublishDirectory");
    }
}
