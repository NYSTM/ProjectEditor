using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace ProjectEditor;

/// <summary>
/// レガシータグクリーンアップ処理を担当するヘルパークラス。
/// .NET Framework 時代の不要な要素の検出・確認・削除を一連の流れで実行します。
/// </summary>
public static class LegacyCleanupHelper
{
    /// <summary>
    /// 指定されたプロジェクトファイル群のレガシータグをクリーンアップします。
    /// 検出 → プレビュー確認 → 削除実行 の順に処理を進めます。
    /// </summary>
    /// <param name="targetFiles">クリーンアップ対象のプロジェクトファイルパスのリスト。</param>
    /// <returns>クリーンアップが実行された場合は <see langword="true"/>、キャンセルまたは対象なしの場合は <see langword="false"/>。</returns>
    public static bool ExecuteCleanup(IEnumerable<string> targetFiles)
    {
        // 全ファイルのレガシー要素を検出する
        var allLegacyElements = DetectLegacyElements(targetFiles);
        if (allLegacyElements == null)
            return false;

        int totalLegacyCount = allLegacyElements.Sum(kvp => kvp.Value.Elements.Count);

        // 対象要素がなければ完了を通知して終了
        if (totalLegacyCount == 0)
        {
            MessageBox.Show("古い要素は検出されませんでした。\nプロジェクトファイルは既にクリーンです。",
                "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        // プレビューを表示してユーザーに実行を確認する
        if (!ShowPreviewAndConfirm(allLegacyElements, totalLegacyCount))
            return false;

        return PerformCleanup(allLegacyElements);
    }

    /// <summary>
    /// 対象ファイルをすべて読み込み、ファイルごとのレガシー要素を検出します。
    /// 読み込みエラーが発生した場合はエラーダイアログを表示して <see langword="null"/> を返します。
    /// </summary>
    /// <param name="targetFiles">検査対象のプロジェクトファイルパスのシーケンス。</param>
    /// <returns>
    /// ファイルパスをキー、読み込み済み <see cref="ProjectModifier"/> と検出された <see cref="LegacyElement"/> のリストを値とする辞書。
    /// 読み込みエラー発生時は <see langword="null"/>。
    /// </returns>
    private static Dictionary<string, (ProjectModifier Modifier, List<LegacyElement> Elements)>? DetectLegacyElements(IEnumerable<string> targetFiles)
    {
        var allLegacyElements = new Dictionary<string, (ProjectModifier Modifier, List<LegacyElement> Elements)>();

        foreach (string file in targetFiles)
        {
            try
            {
                var modifier = new ProjectModifier(file);
                modifier.Load();
                var legacyElements = modifier.DetectLegacyElements();

                // レガシー要素が存在するファイルのみ辞書に追加する
                if (legacyElements.Count > 0)
                {
                    allLegacyElements[file] = (modifier, legacyElements);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの読み込みに失敗しました。{Environment.NewLine}{Path.GetFileName(file)}: {ex.Message}",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        return allLegacyElements;
    }

    /// <summary>
    /// 検出されたレガシー要素のプレビューを表示し、削除実行をユーザーに確認します。
    /// </summary>
    /// <param name="allLegacyElements">ファイルパスと検出要素リストの辞書。</param>
    /// <param name="totalLegacyCount">検出された要素の合計件数。</param>
    /// <returns>ユーザーが「はい」を選択した場合は <see langword="true"/>。</returns>
    private static bool ShowPreviewAndConfirm(Dictionary<string, (ProjectModifier Modifier, List<LegacyElement> Elements)> allLegacyElements, int totalLegacyCount)
    {
        var preview = BuildPreviewMessage(allLegacyElements, totalLegacyCount);

        var result = MessageBox.Show(preview, "レガシータグのクリーンアップ",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// プレビューダイアログに表示するメッセージ文字列を構築します。
    /// ファイル数・要素数が多い場合は先頭5ファイル・要素3件までに省略して表示します。
    /// </summary>
    /// <param name="allLegacyElements">ファイルパスと検出要素リストの辞書。</param>
    /// <param name="totalLegacyCount">検出された要素の合計件数。</param>
    /// <returns>プレビューダイアログに表示するメッセージ文字列。</returns>
    private static string BuildPreviewMessage(Dictionary<string, (ProjectModifier Modifier, List<LegacyElement> Elements)> allLegacyElements, int totalLegacyCount)
    {
        var preview = new System.Text.StringBuilder();
        preview.AppendLine($"合計 {totalLegacyCount} 個の古い要素が検出されました。");
        preview.AppendLine();
        preview.AppendLine("【検出された要素】");

        // 最大5ファイルまでプレビュー表示する
        foreach (var kvp in allLegacyElements.Take(5))
        {
            var fileName = Path.GetFileName(kvp.Key);
            var elements = kvp.Value.Elements;
            preview.AppendLine($"\n■ {fileName} ({elements.Count}個)");

            // 各ファイルの要素は最大3件まで表示する
            foreach (var element in elements.Take(3))
            {
                preview.AppendLine($"  ・{element.ElementName}: {element.AttributeValue}");
            }

            if (elements.Count > 3)
            {
                preview.AppendLine($"  ... 他 {elements.Count - 3} 個");
            }
        }

        // 6ファイル目以降は件数のみ表示する
        if (allLegacyElements.Count > 5)
        {
            preview.AppendLine($"\n... 他 {allLegacyElements.Count - 5} ファイル");
        }

        preview.AppendLine();
        preview.AppendLine("これらの要素を削除しますか？");
        preview.AppendLine();
        preview.AppendLine("⚠ この操作は元に戻せません。");
        preview.AppendLine("⚠ バージョン管理でバックアップを取ることを推奨します。");

        return preview.ToString();
    }

    /// <summary>
    /// 検出されたレガシー要素を各ファイルから実際に削除して保存します。
    /// 削除に失敗したファイルはスキップしてエラーを蓄積します。
    /// </summary>
    /// <param name="allLegacyElements">ファイルパスと削除対象要素リストの辞書。</param>
    /// <returns>1件以上のファイルを処理できた場合は <see langword="true"/>。</returns>
    private static bool PerformCleanup(Dictionary<string, (ProjectModifier Modifier, List<LegacyElement> Elements)> allLegacyElements)
    {
        int cleanedFiles = 0;
        int totalRemoved = 0;
        var errors = new List<string>();

        foreach (var kvp in allLegacyElements)
        {
            try
            {
                // 検出時と同じProjectModifierインスタンスを使用してXElement参照を保持する
                var modifier = kvp.Value.Modifier;
                var elements = kvp.Value.Elements;

                int removed = modifier.RemoveLegacyElements(elements);
                totalRemoved += removed;

                modifier.Save();
                cleanedFiles++;
            }
            catch (Exception ex)
            {
                // 1ファイルの失敗で処理全体を中断しない
                errors.Add($"{Path.GetFileName(kvp.Key)}: {ex.Message}");
            }
        }

        ShowCleanupResult(cleanedFiles, totalRemoved, errors);
        return true;
    }

    /// <summary>
    /// クリーンアップ結果をダイアログで通知します。
    /// エラーがある場合は警告アイコンで表示します。
    /// </summary>
    /// <param name="cleanedFiles">正常に処理できたファイル数。</param>
    /// <param name="totalRemoved">削除できた要素の合計件数。</param>
    /// <param name="errors">処理に失敗したファイルのエラーメッセージリスト。</param>
    private static void ShowCleanupResult(int cleanedFiles, int totalRemoved, List<string> errors)
    {
        var resultMessage = $"{cleanedFiles} ファイルから {totalRemoved} 個の古い要素を削除しました。";

        if (errors.Count > 0)
        {
            resultMessage += $"{Environment.NewLine}{Environment.NewLine}エラー:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
        }

        MessageBox.Show(resultMessage, "クリーンアップ完了",
            MessageBoxButton.OK, errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }
}
