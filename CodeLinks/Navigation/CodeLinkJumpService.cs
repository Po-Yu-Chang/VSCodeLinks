using System;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace CodeLinks.Navigation
{
    /// <summary>
    /// 程式碼連結跳轉服務
    /// </summary>
    internal static class CodeLinkJumpService
    {
        /// <summary>
        /// 跳轉到指定的標記
        /// </summary>
        /// <param name="view">目前的文字檢視</param>
        /// <param name="key">要跳轉的標記鍵值</param>
        public static void JumpTo(ITextView view, string key)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
                if (dte?.Solution == null) return;

                // 建立用來搜尋 tag:#key 的正規表達式
                var regex = new Regex($@"//\s*tag:#?{Regex.Escape(key)}\b", 
                                     RegexOptions.IgnoreCase | RegexOptions.Compiled);

                // 搜尋所有專案中的 C# 檔案
                foreach (Project project in dte.Solution.Projects)
                {
                    if (SearchInProject(project, regex, key))
                        return;
                }

                // 如果找不到，顯示訊息
                ShowNotFoundMessage(key);
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"跳轉時發生錯誤: {ex.Message}");
            }
        }

        private static bool SearchInProject(Project project, Regex regex, string key)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                return SearchInProjectItems(project.ProjectItems, regex, key);
            }
            catch
            {
                return false;
            }
        }

        private static bool SearchInProjectItems(ProjectItems items, Regex regex, string key)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem item in items)
            {
                try
                {
                    // 遞迴搜尋子項目
                    if (item.ProjectItems?.Count > 0)
                    {
                        if (SearchInProjectItems(item.ProjectItems, regex, key))
                            return true;
                    }

                    // 檢查是否為 C# 檔案
                    if (item.Kind == Constants.vsProjectItemKindPhysicalFile && 
                        item.Name?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (SearchInFile(item, regex))
                            return true;
                    }
                }
                catch
                {
                    // 忽略單一檔案的錯誤，繼續搜尋其他檔案
                    continue;
                }
            }

            return false;
        }

        private static bool SearchInFile(ProjectItem item, Regex regex)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var window = item.Open(Constants.vsViewKindTextView);
                if (window?.Document is TextDocument textDoc)
                {
                    var text = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                    var match = regex.Match(text);

                    if (match.Success)
                    {
                        window.Activate();
                        
                        // 計算行號和列號
                        var lines = text.Substring(0, match.Index).Split('\n');
                        var lineNumber = lines.Length;
                        var columnNumber = lines[lines.Length - 1].Length + 1;

                        // 移動游標到匹配位置
                        textDoc.Selection.MoveToLineAndOffset(lineNumber, columnNumber);
                        textDoc.Selection.SelectLine();
                        
                        return true;
                    }
                }
            }
            catch
            {
                // 忽略檔案開啟錯誤
            }

            return false;
        }

        private static void ShowNotFoundMessage(string key)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                $"找不到標記: tag:#{key}",
                "CodeLinks",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private static void ShowErrorMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                message,
                "CodeLinks 錯誤",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
