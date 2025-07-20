using System;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Task = System.Threading.Tasks.Task;

namespace CodeLinks.Commands
{
    /// <summary>
    /// 跳轉到標籤命令實作
    /// </summary>
    internal sealed class JumpCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("87654321-4321-8765-cba9-210987654321");

        private readonly AsyncPackage package;

        private JumpCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static JumpCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get { return this.package; }
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new JumpCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                System.Diagnostics.Debug.WriteLine("CodeLinks: Jump command executed");

                // 取得 DTE 服務
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Selection == null)
                {
                    ShowMessage("無法取得當前文檔");
                    return;
                }

                var selection = dte.ActiveDocument.Selection as TextSelection;
                var currentLine = selection.ActivePoint.Line;
                
                // 取得當前行內容
                selection.GotoLine(currentLine);
                selection.SelectLine();
                var lineText = selection.Text.Trim();
                
                System.Diagnostics.Debug.WriteLine($"CodeLinks: Current line: {lineText}");

                // 檢查是否包含 goto:#key
                var match = Regex.Match(lineText, @"//\s*goto:#(?<key>\w+)", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    ShowMessage("當前行沒有 goto:#key 標記");
                    return;
                }

                var key = match.Groups["key"].Value;
                System.Diagnostics.Debug.WriteLine($"CodeLinks: Found goto:#{key}");

                // 搜尋對應的 tag
                if (FindAndJumpToTag(dte, key))
                {
                    ShowMessage($"跳轉到 tag:#{key} 成功");
                }
                else
                {
                    ShowMessage($"找不到 tag:#{key}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CodeLinks: Command error: {ex.Message}");
                ShowMessage($"跳轉錯誤: {ex.Message}");
            }
        }

        private bool FindAndJumpToTag(DTE dte, string key)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                var tagPattern = $@"//\s*tag:#{Regex.Escape(key)}\b";
                var regex = new Regex(tagPattern, RegexOptions.IgnoreCase);

                // 先搜尋當前文檔
                var activeDoc = dte.ActiveDocument;
                if (activeDoc != null)
                {
                    var textDoc = activeDoc.Object("TextDocument") as TextDocument;
                    if (textDoc != null)
                    {
                        var editPoint = textDoc.StartPoint.CreateEditPoint();
                        var docText = editPoint.GetText(textDoc.EndPoint);
                        
                        var match = regex.Match(docText);
                        if (match.Success)
                        {
                            return JumpToPosition(activeDoc, docText, match.Index);
                        }
                    }
                }

                // 搜尋其他文檔
                if (dte.Solution?.Projects != null)
                {
                    foreach (Project project in dte.Solution.Projects)
                    {
                        if (SearchProjectForTag(project, regex))
                            return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CodeLinks: FindAndJumpToTag error: {ex}");
                return false;
            }
        }

        private bool SearchProjectForTag(Project project, Regex regex)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                return SearchProjectItems(project.ProjectItems, regex);
            }
            catch
            {
                return false;
            }
        }

        private bool SearchProjectItems(ProjectItems items, Regex regex)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                foreach (ProjectItem item in items)
                {
                    try
                    {
                        // 遞迴搜尋子項目
                        if (item.ProjectItems?.Count > 0)
                        {
                            if (SearchProjectItems(item.ProjectItems, regex))
                                return true;
                        }

                        // 檢查 C# 檔案
                        if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile && 
                            item.Name?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            var window = item.Open(EnvDTE.Constants.vsViewKindTextView);
                            var textDoc = window.Document.Object("TextDocument") as TextDocument;
                            
                            if (textDoc != null)
                            {
                                var editPoint = textDoc.StartPoint.CreateEditPoint();
                                var docText = editPoint.GetText(textDoc.EndPoint);
                                var match = regex.Match(docText);
                                
                                if (match.Success)
                                {
                                    window.Activate();
                                    return JumpToPosition(window.Document, docText, match.Index);
                                }
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
            }
            
            return false;
        }

        private bool JumpToPosition(Document document, string docText, int position)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                // 計算行號
                var textBeforePosition = docText.Substring(0, position);
                var lineNumber = 1;
                
                for (int i = 0; i < textBeforePosition.Length; i++)
                {
                    if (textBeforePosition[i] == '\n')
                        lineNumber++;
                }
                
                var selection = document.Selection as TextSelection;
                if (selection != null)
                {
                    selection.GotoLine(lineNumber);
                    selection.SelectLine();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CodeLinks: JumpToPosition error: {ex}");
                return false;
            }
        }

        private void ShowMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            VsShellUtilities.ShowMessageBox(
                Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShell)) as IServiceProvider,
                message,
                "CodeLinks",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}