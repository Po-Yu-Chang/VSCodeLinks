/// <summary>
/// CodeLinks - 高效能版本
/// 
/// 使用索引快取解決大專案效能問題：
/// - 懶載入索引建立，第一次使用時才掃描專案
/// - 使用 ConcurrentDictionary 快取所有標籤位置
/// - 點擊跳轉只需 O(1) 字典查詢，無需重複掃描
/// - 支援索引動態更新（可選）
/// 
/// 版本：v1.2.0 - 高效能版本
/// 作者：Po-Yu-Chang
/// </summary>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace CodeLinks
{
    /// <summary>
    /// 標籤位置記錄 - 輕量級資料結構
    /// </summary>
    public class TagLocation
    {
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }

        public TagLocation(string filePath, int line, int column)
        {
            FilePath = filePath;
            Line = line;
            Column = column;
        }

        public override bool Equals(object obj)
        {
            var other = obj as TagLocation;
            if (other != null)
            {
                return FilePath == other.FilePath && Line == other.Line && Column == other.Column;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (FilePath != null ? FilePath.GetHashCode() : 0) ^ Line ^ Column;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}", FilePath, Line, Column);
        }
    }

    /// <summary>
    /// 高效能標籤索引管理器 - 單例模式
    /// </summary>
    internal static class TagIndexManager
    {
        /// <summary>
        /// 標籤索引：key -> 標籤位置列表
        /// 使用 ConcurrentDictionary 支援多執行緒安全存取
        /// </summary>
        private static readonly ConcurrentDictionary<string, List<TagLocation>> TagIndex = new ConcurrentDictionary<string, List<TagLocation>>();
        
        /// <summary>
        /// 建置鎖：確保索引只被建立一次
        /// </summary>
        private static readonly SemaphoreSlim BuildLock = new SemaphoreSlim(1, 1);
        
        /// <summary>
        /// 索引是否已建立
        /// </summary>
        private static volatile bool _indexBuilt = false;
        
        /// <summary>
        /// 標籤匹配的正規表達式
        /// </summary>
        private static readonly Regex TagRegex = new Regex(@"//\s*tag:#(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 確保索引已建立（懶載入）
        /// 只在第一次需要時才建立索引，避免啟動時的效能負擔
        /// </summary>
        public static async Task EnsureIndexAsync(string projectRoot, CancellationToken cancellationToken = default)
        {
            // 快速檢查：如果已建立則直接返回
            if (_indexBuilt) return;
            
            // 取得建置鎖，確保只有一個執行緒建立索引
            await BuildLock.WaitAsync(cancellationToken);
            try
            {
                // 雙重檢查：可能在等待鎖的期間已被其他執行緒建立
                if (_indexBuilt) return;
                
                System.Diagnostics.Debug.WriteLine("CodeLinks: Building tag index...");
                var startTime = DateTime.UtcNow;
                
                await BuildIndexAsync(projectRoot, cancellationToken);
                
                _indexBuilt = true;
                var duration = DateTime.UtcNow - startTime;
                System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Index built in {0:F0}ms, found {1} unique tags", duration.TotalMilliseconds, TagIndex.Count));
            }
            finally
            {
                BuildLock.Release();
            }
        }

        /// <summary>
        /// 建立標籤索引
        /// 並行掃描所有支援的檔案類型，提升建置速度
        /// </summary>
        private static async Task BuildIndexAsync(string projectRoot, CancellationToken cancellationToken)
        {
            // 支援的檔案類型
            var extensions = new[] { ".cs", ".vb", ".js", ".ts", ".txt", ".xml", ".html", ".css", ".cpp", ".h", ".py", ".java" };
            
            // 收集所有要掃描的檔案
            var allFiles = new List<string>();
            foreach (var ext in extensions)
            {
                try
                {
                    var files = Directory.GetFiles(projectRoot, "*" + ext, SearchOption.AllDirectories);
                    allFiles.AddRange(files);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Error scanning {0} files: {1}", ext, ex.Message));
                }
            }

            System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Scanning {0} files...", allFiles.Count));

            // 並行處理檔案，提升效能
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2); // 限制並行數量避免資源耗盡
            var tasks = allFiles.Select(async file =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await ScanFileAsync(file, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 掃描單一檔案中的標籤
        /// </summary>
        private static Task ScanFileAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                // 使用同步讀取，因為 ReadAllLinesAsync 在 .NET Framework 4.7.2 中不存在
                var lines = File.ReadAllLines(filePath);
                
                for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                {
                    var line = lines[lineNumber];
                    var match = TagRegex.Match(line);
                    
                    if (match.Success)
                    {
                        var key = match.Groups[1].Value;
                        var location = new TagLocation(filePath, lineNumber, match.Index);
                        
                        // 執行緒安全地更新索引
                        TagIndex.AddOrUpdate(
                            key,
                            new List<TagLocation> { location },
                            (_, existingList) =>
                            {
                                lock (existingList) // 保護 List 的執行緒安全
                                {
                                    existingList.Add(location);
                                    return existingList;
                                }
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Error scanning file {0}: {1}", filePath, ex.Message));
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// 查找標籤位置
        /// O(1) 字典查詢，極快速度
        /// </summary>
        public static TagLocation FindTag(string key)
        {
            if (!TagIndex.TryGetValue(key, out var locations) || locations.Count == 0)
                return null;

            // 返回第一個找到的位置
            // 如果有多個同名標籤，可以考慮更智慧的選擇邏輯
            lock (locations)
            {
                return locations.FirstOrDefault();
            }
        }

        /// <summary>
        /// 更新單一檔案的索引（用於檔案變更時）
        /// </summary>
        public static async Task UpdateFileIndexAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!_indexBuilt) return; // 如果索引還沒建立，不需要更新

            try
            {
                // 移除該檔案的所有舊索引項
                var keysToUpdate = new List<string>();
                foreach (var kvp in TagIndex)
                {
                    lock (kvp.Value)
                    {
                        if (kvp.Value.RemoveAll(loc => loc.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)) > 0)
                        {
                            if (kvp.Value.Count == 0)
                                keysToUpdate.Add(kvp.Key);
                        }
                    }
                }

                // 移除空的索引項
                foreach (var key in keysToUpdate)
                {
                    TagIndex.TryRemove(key, out _);
                }

                // 重新掃描該檔案
                if (File.Exists(filePath))
                {
                    await ScanFileAsync(filePath, cancellationToken);
                }

                System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Updated index for {0}", filePath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Error updating index for {0}: {1}", filePath, ex.Message));
            }
        }

        /// <summary>
        /// 清除索引（用於重置）
        /// </summary>
        public static void ClearIndex()
        {
            TagIndex.Clear();
            _indexBuilt = false;
            System.Diagnostics.Debug.WriteLine("CodeLinks: Index cleared");
        }

        /// <summary>
        /// 取得索引統計資訊
        /// </summary>
        public static (int TagCount, int LocationCount) GetIndexStats()
        {
            var locationCount = TagIndex.Values.Sum(list =>
            {
                lock (list)
                {
                    return list.Count;
                }
            });
            return (TagIndex.Count, locationCount);
        }
    }

    /// <summary>
    /// 高效能標記器提供者
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(ITextMarkerTag))]
    internal sealed class PerformantTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null) return null;
            return new PerformantTagger(buffer) as ITagger<T>;
        }
    }

    /// <summary>
    /// 高效能標記器 - 只負責語法高亮
    /// </summary>
    internal sealed class PerformantTagger : ITagger<ITextMarkerTag>
    {
        private readonly ITextBuffer _buffer;
        private static readonly Regex TagRegex = new Regex(@"//\s*tag:#(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex GotoRegex = new Regex(@"//\s*goto:#(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PerformantTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            System.Diagnostics.Debug.WriteLine("CodeLinks v1.2.0: Performant tagger created");
        }

        public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || _buffer == null)
                yield break;

            foreach (var span in spans)
            {
                var text = span.GetText();
                
                // 效能最佳化：快速檢查
                if (!text.Contains("tag:#") && !text.Contains("goto:#"))
                    continue;
                
                // 標記 tag (藍色)
                foreach (Match match in TagRegex.Matches(text))
                {
                    var tagSpan = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                    yield return new TagSpan<ITextMarkerTag>(tagSpan, new TextMarkerTag("blue"));
                }
                
                // 標記 goto (綠色)
                foreach (Match match in GotoRegex.Matches(text))
                {
                    var tagSpan = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                    yield return new TagSpan<ITextMarkerTag>(tagSpan, new TextMarkerTag("green"));
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { /* 暫時不實作 */ }
            remove { /* 暫時不實作 */ }
        }
    }

    /// <summary>
    /// 高效能滑鼠處理器提供者
    /// </summary>
    [Export(typeof(IMouseProcessorProvider))]
    [Name("PerformantMouseProcessor")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class PerformantMouseProcessorProvider : IMouseProcessorProvider
    {
        public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            return new PerformantMouseProcessor(wpfTextView);
        }
    }

    /// <summary>
    /// 高效能滑鼠處理器 - 使用索引快取實現超快跳轉
    /// </summary>
    internal sealed class PerformantMouseProcessor : MouseProcessorBase
    {
        private readonly IWpfTextView _textView;
        private static readonly Regex GotoRegex = new Regex(@"//\s*goto:#(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PerformantMouseProcessor(IWpfTextView textView)
        {
            _textView = textView;
        }

        public override void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 2)
                {
                    System.Diagnostics.Debug.WriteLine("CodeLinks: Double click detected (performant version)");

                    var position = _textView.Caret.Position.BufferPosition;
                    var line = position.GetContainingLine();
                    var lineText = line.GetText();

                    if (lineText.Contains("goto:#"))
                    {
                        var matches = GotoRegex.Matches(lineText);
                        foreach (Match match in matches)
                        {
                            var key = match.Groups[1].Value;
                            System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Found goto key: {0}", key));

                            // 非同步執行導航，避免阻塞 UI
                            _ = Task.Run(async () => await NavigateToTagAsync(key));
                            
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Mouse processor error: {0}", ex.Message));
            }

            base.PreprocessMouseLeftButtonDown(e);
        }

        /// <summary>
        /// 非同步導航到標籤
        /// 使用索引快取實現高效能跳轉
        /// </summary>
        private async Task NavigateToTagAsync(string key)
        {
            try
            {
                // 1. 先檢查當前檔案（同步，很快）
                var currentFileTarget = FindTagInCurrentFile(key);
                if (currentFileTarget.HasValue)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    _textView.Caret.MoveTo(currentFileTarget.Value);
                    _textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(currentFileTarget.Value, 0));
                    System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Found in current file: {0}", key));
                    return;
                }

                // 2. 確保索引已建立
                var projectRoot = GetProjectRoot();
                if (projectRoot != null)
                {
                    await TagIndexManager.EnsureIndexAsync(projectRoot);
                    
                    // 3. 使用索引查找（超快速 O(1)）
                    var target = TagIndexManager.FindTag(key);
                    if (target != null)
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        await NavigateToFileAsync(target.FilePath, target.Line, target.Column);
                        System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Found in index: {0}:{1}", target.FilePath, target.Line));
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Tag '{0}' not found", key));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Navigation error: {0}", ex.Message));
            }
        }

        /// <summary>
        /// 在當前檔案中搜尋標籤（同步，快速）
        /// </summary>
        private SnapshotPoint? FindTagInCurrentFile(string key)
        {
            var snapshot = _textView.TextBuffer.CurrentSnapshot;
            var targetPattern = $@"//\s*tag:#{Regex.Escape(key)}\b";
            var regex = new Regex(targetPattern, RegexOptions.IgnoreCase);

            for (int i = 0; i < snapshot.LineCount; i++)
            {
                var line = snapshot.GetLineFromLineNumber(i);
                var lineText = line.GetText();
                var match = regex.Match(lineText);
                if (match.Success)
                {
                    return line.Start + match.Index;
                }
            }
            return null;
        }

        /// <summary>
        /// 取得專案根目錄
        /// </summary>
        private string GetProjectRoot()
        {
            try
            {
                if (!_textView.TextBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var textDocument) ||
                    textDocument?.FilePath == null)
                    return null;

                var currentDir = Path.GetDirectoryName(textDocument.FilePath);
                var dir = currentDir;

                while (dir != null)
                {
                    if (Directory.GetFiles(dir, "*.csproj").Any() ||
                        Directory.GetFiles(dir, "*.vbproj").Any() ||
                        Directory.GetFiles(dir, "*.sln").Any() ||
                        Directory.Exists(Path.Combine(dir, ".git")))
                    {
                        return dir;
                    }

                    var parent = Directory.GetParent(dir);
                    dir = parent?.FullName;
                }

                return currentDir;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 非同步導航到檔案
        /// </summary>
        private async Task NavigateToFileAsync(string filePath, int line, int column)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte != null)
                {
                    var window = dte.ItemOperations.OpenFile(filePath);
                    if (window?.Document?.Selection is EnvDTE.TextSelection selection)
                    {
                        selection.GotoLine(line + 1, true);
                        selection.MoveToLineAndOffset(line + 1, column + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("CodeLinks: Navigate to file error: {0}", ex.Message));
            }
        }
    }
}