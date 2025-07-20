# Visual Studio 擴充套件重新建立指南

## 步驟 1: 重置專案到乾淨狀態

1. 開啟命令提示字元或 PowerShell
2. 切換到專案目錄：
   ```
   cd c:\Users\qoose\Desktop\VSCodeLinks
   ```

3. 執行 Git 重置（回到最初狀態）：
   ```
   git reset --hard 88c87b1
   git clean -fd
   ```

## 步驟 2: 使用 Visual Studio 建立正確的 VSIX 專案

1. **開啟 Visual Studio 2022**
2. **建立新專案**：
   - 檔案 → 新增 → 專案
   - 搜尋 "VSIX"
   - 選擇 "VSIX Project" 範本
   - 專案名稱：CodeLinks
   - 位置：選擇已存在的 VSCodeLinks 資料夾

3. **新增 MEF 擴充功能項目**：
   - 在 CodeLinks 專案上按右鍵
   - 新增 → 新增項目
   - 選擇 "Extensibility" 類別
   - 選擇 "Editor Classifier" 或 "Editor Margin"

## 步驟 3: 實作程式碼標籤功能

### 3.1 建立 Tagger（標籤處理器）

建立 `CodeLinkTagger.cs`：

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace CodeLinks
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("CSharp")]
    [TagType(typeof(CodeLinkTag))]
    internal sealed class CodeLinkTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                () => new CodeLinkTagger(buffer)) as ITagger<T>;
        }
    }

    public class CodeLinkTag : ITag
    {
        public string Key { get; }
        public string Type { get; } // "tag" or "goto"

        public CodeLinkTag(string key, string type)
        {
            Key = key;
            Type = type;
        }
    }

    internal sealed class CodeLinkTagger : ITagger<CodeLinkTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly Regex _pattern = new Regex(@"//\s*(?<type>tag|goto):#(?<key>\w+)");

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public CodeLinkTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.Changed += OnBufferChanged;
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            var snapshot = e.After;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        public IEnumerable<ITagSpan<CodeLinkTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                var text = span.GetText();
                foreach (Match match in _pattern.Matches(text))
                {
                    var type = match.Groups["type"].Value;
                    var key = match.Groups["key"].Value;
                    var matchSpan = new SnapshotSpan(span.Start + match.Index, match.Length);
                    
                    yield return new TagSpan<CodeLinkTag>(matchSpan, new CodeLinkTag(key, type));
                }
            }
        }
    }
}
```

### 3.2 建立 Classification（語法高亮）

建立 `CodeLinkClassifier.cs`：

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace CodeLinks
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("CSharp")]
    internal sealed class CodeLinkClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry { get; set; }

        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactory { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                () => new CodeLinkClassifier(textBuffer, TagAggregatorFactory, ClassificationRegistry));
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "CodeLink.Tag")]
    [Name("CodeLink.Tag")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class CodeLinkTagFormat : ClassificationFormatDefinition
    {
        public CodeLinkTagFormat()
        {
            DisplayName = "Code Link Tag";
            ForegroundColor = Colors.DarkBlue;
            IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "CodeLink.Goto")]
    [Name("CodeLink.Goto")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class CodeLinkGotoFormat : ClassificationFormatDefinition
    {
        public CodeLinkGotoFormat()
        {
            DisplayName = "Code Link Goto";
            ForegroundColor = Colors.DarkGreen;
            IsBold = true;
        }
    }

    internal static class CodeLinkClassificationTypeNames
    {
        public const string Tag = "CodeLink.Tag";
        public const string Goto = "CodeLink.Goto";
    }

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(CodeLinkClassificationTypeNames.Tag)]
    internal static class CodeLinkTagClassificationDefinition { }

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(CodeLinkClassificationTypeNames.Goto)]
    internal static class CodeLinkGotoClassificationDefinition { }

    internal sealed class CodeLinkClassifier : IClassifier
    {
        private readonly ITagAggregator<CodeLinkTag> _tagAggregator;
        private readonly IClassificationTypeRegistryService _classificationRegistry;

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public CodeLinkClassifier(ITextBuffer buffer, 
            IViewTagAggregatorFactoryService tagAggregatorFactory,
            IClassificationTypeRegistryService classificationRegistry)
        {
            _tagAggregator = tagAggregatorFactory.CreateTagAggregator<CodeLinkTag>(buffer);
            _classificationRegistry = classificationRegistry;
            _tagAggregator.TagsChanged += OnTagsChanged;
        }

        private void OnTagsChanged(object sender, TagsChangedEventArgs e)
        {
            ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(e.Span));
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var result = new List<ClassificationSpan>();

            foreach (var tagSpan in _tagAggregator.GetTags(span))
            {
                var classificationType = _classificationRegistry.GetClassificationType(
                    tagSpan.Tag.Type == "tag" ? CodeLinkClassificationTypeNames.Tag : CodeLinkClassificationTypeNames.Goto);
                
                result.Add(new ClassificationSpan(tagSpan.Span, classificationType));
            }

            return result;
        }
    }
}
```

### 3.3 建立跳轉命令

建立 `JumpCommand.cs`：

```csharp
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CodeLinks
{
    internal sealed class JumpCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("f1d1c7d3-6c7a-4f3e-8c3e-3f1d1c7d6c7a");

        private readonly Package _package;

        private JumpCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static JumpCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new JumpCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte?.ActiveDocument?.Selection == null) return;

                var selection = dte.ActiveDocument.Selection as EnvDTE.TextSelection;
                var currentLine = selection.ActivePoint.Line;
                
                selection.GotoLine(currentLine);
                selection.SelectLine();
                var lineText = selection.Text;

                var match = Regex.Match(lineText, @"//\s*goto:#(?<key>\w+)");
                if (match.Success)
                {
                    var key = match.Groups["key"].Value;
                    if (FindAndJumpToTag(dte, key))
                    {
                        VsShellUtilities.ShowMessageBox(ServiceProvider, $"跳轉到 tag:#{key} 成功", "CodeLinks", 
                            OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    else
                    {
                        VsShellUtilities.ShowMessageBox(ServiceProvider, $"找不到 tag:#{key}", "CodeLinks", 
                            OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, $"跳轉錯誤: {ex.Message}", "CodeLinks", 
                    OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private bool FindAndJumpToTag(EnvDTE.DTE dte, string key)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var tagPattern = $@"//\s*tag:#{Regex.Escape(key)}\b";
            var regex = new Regex(tagPattern, RegexOptions.IgnoreCase);

            // 搜尋當前文件
            var activeDoc = dte.ActiveDocument;
            if (activeDoc != null)
            {
                var textDoc = activeDoc.Object("TextDocument") as EnvDTE.TextDocument;
                if (textDoc != null && FindTagInDocument(textDoc, regex, activeDoc.Selection))
                    return true;
            }

            // 搜尋專案中的其他檔案
            foreach (EnvDTE.Project project in dte.Solution.Projects)
            {
                if (SearchProjectForTag(project, regex, dte))
                    return true;
            }

            return false;
        }

        private bool FindTagInDocument(EnvDTE.TextDocument textDoc, Regex regex, EnvDTE.TextSelection selection)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var docText = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
            var match = regex.Match(docText);

            if (match.Success)
            {
                var lines = docText.Substring(0, match.Index).Split('\n');
                var lineNumber = lines.Length;
                selection.GotoLine(lineNumber);
                return true;
            }

            return false;
        }

        private bool SearchProjectForTag(EnvDTE.Project project, Regex regex, EnvDTE.DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                foreach (EnvDTE.ProjectItem item in project.ProjectItems)
                {
                    if (SearchProjectItemForTag(item, regex, dte))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private bool SearchProjectItemForTag(EnvDTE.ProjectItem item, Regex regex, EnvDTE.DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile && 
                    item.Name?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var window = item.Open(EnvDTE.Constants.vsViewKindTextView);
                    var textDoc = window.Document.Object("TextDocument") as EnvDTE.TextDocument;

                    if (textDoc != null && FindTagInDocument(textDoc, regex, window.Document.Selection))
                    {
                        window.Activate();
                        return true;
                    }
                }

                foreach (EnvDTE.ProjectItem subItem in item.ProjectItems)
                {
                    if (SearchProjectItemForTag(subItem, regex, dte))
                        return true;
                }
            }
            catch { }

            return false;
        }
    }
}
```

## 步驟 4: 建立套件類別

建立主要的套件類別 `CodeLinksPackage.cs`：

```csharp
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodeLinks
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CodeLinksPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class CodeLinksPackage : AsyncPackage
    {
        public const string PackageGuidString = "12345678-1234-5678-9abc-123456789012";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JumpCommand.InitializeAsync(this);
        }
    }
}
```

## 步驟 5: 更新 VSIX 清單

確保 `source.extension.vsixmanifest` 檔案正確設定：

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
  <Metadata>
    <Identity Id="CodeLinks.12345678-1234-5678-9abc-123456789012" Version="1.0" Language="en-US" Publisher="YourName" />
    <DisplayName>CodeLinks</DisplayName>
    <Description>Visual Studio extension for code navigation using // tag:#key and // goto:#key</Description>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Microsoft.VisualStudio.Community" Version="[17.0,18.0)" />
  </Installation>
  <Dependencies>
    <Dependency Id="Microsoft.Framework.NDP" DisplayName="Microsoft .NET Framework" Version="[4.7.2,)" />
  </Dependencies>
  <Prerequisites>
    <Prerequisite Id="Microsoft.VisualStudio.Component.CoreEditor" Version="[17.0,18.0)" />
  </Prerequisites>
  <Assets>
    <Asset Type="Microsoft.VisualStudio.VsPackage" d:Source="Project" d:ProjectName="%CurrentProject%" Path="|%CurrentProject%;PkgdefProjectOutputGroup|" />
    <Asset Type="Microsoft.VisualStudio.MefComponent" d:Source="Project" d:ProjectName="%CurrentProject%" Path="|%CurrentProject%|" />
  </Assets>
</PackageManifest>
```

## 步驟 6: 建置與測試

1. 建置專案（F6）
2. 啟動除錯（F5）- 這會開啟 Visual Studio 實驗執行個體
3. 在實驗執行個體中開啟 C# 檔案
4. 測試 `// tag:#test` 和 `// goto:#test` 功能

這個方法遵循 Microsoft 官方最佳實踐，使用正確的 MEF 架構和 Visual Studio SDK 模式。
