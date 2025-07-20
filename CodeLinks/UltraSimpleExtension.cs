/// <summary>
/// CodeLinks - Visual Studio 擴充功能
/// 
/// 輕量級的程式碼導航工具，提供便利的標籤跳轉功能：
/// - 📍 使用 // tag:#標籤名稱 建立定位點（藍色標記）
/// - 🔗 使用 // goto:#標籤名稱 建立跳轉連結（綠色標記）
/// - 🖱️ 雙擊 goto 標記即可跳轉到對應的定位點
/// - ⚡ 支援同檔案內跳轉和跨檔案跳轉
/// - 🎯 純 MEF 架構，穩定可靠，無外部相依
/// 
/// 版本：v1.1.0
/// 作者：Po-Yu-Chang
/// 授權：MIT License
/// </summary>

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace CodeLinks
{
    /// <summary>
    /// CodeLinks 擴充功能常數定義
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// 擴充功能版本號
        /// </summary>
        public const string Version = "1.1.0";
        
        /// <summary>
        /// 擴充功能名稱
        /// </summary>
        public const string Name = "CodeLinks";
        
        /// <summary>
        /// 標籤定義的正規表達式模式
        /// </summary>
        public const string TagPattern = @"//\s*tag:#(\w+)";
        
        /// <summary>
        /// 跳轉指令的正規表達式模式
        /// </summary>
        public const string GotoPattern = @"//\s*goto:#(\w+)";
    }

    /// <summary>
    /// 標記器提供者 - 負責建立文字標記器
    /// 這是 Visual Studio 擴展的入口點，用於建立文字標記功能
    /// 支援所有文字類型的檔案，並提供 ITextMarkerTag 類型的標記
    /// </summary>
    [Export(typeof(ITaggerProvider))] // 導出為 MEF 組件，讓 VS 能夠發現此提供者
    [ContentType("text")] // 適用於所有文字內容類型
    [TagType(typeof(ITextMarkerTag))] // 指定此提供者建立的標記類型
    internal sealed class UltraSimpleTaggerProvider : ITaggerProvider
    {
        /// <summary>
        /// 建立標記器實例
        /// 當 Visual Studio 需要為特定文字緩衝區建立標記器時會呼叫此方法
        /// </summary>
        /// <typeparam name="T">標記類型，必須實作 ITag 介面</typeparam>
        /// <param name="buffer">要處理的文字緩衝區</param>
        /// <returns>對應的標記器實例，如果無法建立則回傳 null</returns>
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null) return null;
            return new UltraSimpleTagger(buffer) as ITagger<T>;
        }
    }

    /// <summary>
    /// 文字標記器 - 負責識別和標記程式碼中的特殊註解
    /// 支援兩種類型的標記：
    /// 1. tag:#key - 定義標籤位置（藍色標記）
    /// 2. goto:#key - 跳轉指令（綠色標記）
    /// </summary>
    internal sealed class UltraSimpleTagger : ITagger<ITextMarkerTag>
    {
        private readonly ITextBuffer _buffer; // 關聯的文字緩衝區
        
        // 編譯時正規表達式，提升效能
        // 匹配格式：// tag:#標籤名稱（定義標籤）
        private static readonly Regex TagRegex = new Regex(Constants.TagPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // 匹配格式：// goto:#標籤名稱（跳轉到標籤）
        private static readonly Regex GotoRegex = new Regex(Constants.GotoPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 建構函式 - 初始化標記器
        /// </summary>
        /// <param name="buffer">要處理的文字緩衝區</param>
        public UltraSimpleTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            System.Diagnostics.Debug.WriteLine($"{Constants.Name} v{Constants.Version}: Tagger created");
        }

        /// <summary>
        /// 取得指定範圍內的所有標記
        /// 這是 ITagger 介面的核心方法，Visual Studio 會呼叫此方法來取得需要標記的文字範圍
        /// </summary>
        /// <param name="spans">要檢查的文字範圍集合</param>
        /// <returns>標記範圍的集合，每個標記包含位置和樣式資訊</returns>
        public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            // 安全性檢查
            if (spans == null || _buffer == null)
                yield break;

            foreach (var span in spans)
            {
                var text = span.GetText();
                
                // 效能最佳化：快速檢查是否包含目標字串
                if (!text.Contains("tag:#") && !text.Contains("goto:#"))
                    continue;
                
                // 搜尋 tag:#key 模式並標記為藍色
                // 用於標示程式碼中的錨點位置
                foreach (Match match in TagRegex.Matches(text))
                {
                    var tagSpan = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                    yield return new TagSpan<ITextMarkerTag>(tagSpan, new TextMarkerTag("blue"));
                }
                
                // 搜尋 goto:#key 模式並標記為綠色
                // 用於標示可點擊的跳轉連結
                foreach (Match match in GotoRegex.Matches(text))
                {
                    var tagSpan = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                    yield return new TagSpan<ITextMarkerTag>(tagSpan, new TextMarkerTag("green"));
                }
            }
        }

        /// <summary>
        /// 標記變更事件 - 當標記需要重新整理時觸發
        /// 目前實作中未使用，但為 ITagger 介面的必要成員
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }

    /// <summary>
    /// 滑鼠處理器提供者 - 負責建立滑鼠事件處理器
    /// 註冊為 MEF 組件，讓 Visual Studio 能夠自動載入滑鼠事件處理功能
    /// 僅適用於文件檢視角色，避免在不必要的檢視中啟用
    /// </summary>
    [Export(typeof(IMouseProcessorProvider))] // 導出為 MEF 組件
    [Name("UltraSimpleMouseProcessor")] // 處理器名稱，用於識別和偵錯
    [ContentType("text")] // 適用於所有文字內容
    [TextViewRole(PredefinedTextViewRoles.Document)] // 僅在文件檢視中啟用
    internal sealed class UltraSimpleMouseProcessorProvider : IMouseProcessorProvider
    {
        /// <summary>
        /// 為指定的文字檢視建立滑鼠處理器
        /// </summary>
        /// <param name="wpfTextView">WPF 文字檢視元件</param>
        /// <returns>關聯的滑鼠處理器實例</returns>
        public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            return new UltraSimpleMouseProcessor(wpfTextView);
        }
    }

    /// <summary>
    /// 滑鼠事件處理器 - 實作程式碼導航功能
    /// 監聽滑鼠雙擊事件，當使用者雙擊 goto:#key 註解時執行跳轉
    /// 支援同檔案內跳轉和跨檔案跳轉功能
    /// </summary>
    internal sealed class UltraSimpleMouseProcessor : MouseProcessorBase
    {
        private readonly IWpfTextView _textView; // 關聯的文字檢視
        
        // 用於匹配 goto 指令的正規表達式
        private static readonly Regex GotoRegex = new Regex(Constants.GotoPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 建構函式 - 初始化滑鼠處理器
        /// </summary>
        /// <param name="textView">要處理滑鼠事件的文字檢視</param>
        public UltraSimpleMouseProcessor(IWpfTextView textView)
        {
            _textView = textView;
        }

        /// <summary>
        /// 處理滑鼠左鍵按下事件
        /// 主要功能：偵測雙擊事件並執行程式碼導航
        /// 導航邏輯：
        /// 1. 偵測雙擊事件
        /// 2. 檢查點擊位置是否在 goto:#key 註解上
        /// 3. 解析標籤名稱
        /// 4. 優先在當前檔案中搜尋對應的 tag:#key
        /// 5. 如果當前檔案沒找到，則搜尋專案中的其他檔案
        /// 6. 執行跳轉到目標位置
        /// </summary>
        /// <param name="e">滑鼠事件參數</param>
        public override void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            try
            {
                // 檢查是否是雙擊事件
                if (e.ClickCount == 2)
                {
                    System.Diagnostics.Debug.WriteLine($"{Constants.Name}: Double click detected");

                    // 取得游標位置和所在行的文字
                    var position = _textView.Caret.Position.BufferPosition;
                    var line = position.GetContainingLine();
                    var lineText = line.GetText();

                    System.Diagnostics.Debug.WriteLine($"{Constants.Name}: Line text: {lineText}");

                    // 檢查該行是否包含 goto 指令
                    if (lineText.Contains("goto:#"))
                    {
                        var matches = GotoRegex.Matches(lineText);
                        foreach (Match match in matches)
                        {
                            // 提取標籤名稱
                            var key = match.Groups[1].Value;
                            System.Diagnostics.Debug.WriteLine($"{Constants.Name}: Found goto key: {key}");

                            // 步驟1: 在當前檔案中搜尋對應的標籤
                            var targetPos = FindTagInBuffer(key);
                            if (targetPos.HasValue)
                            {
                                System.Diagnostics.Debug.WriteLine($"{Constants.Name}: Navigating to tag in current file: {key}");
                                _textView.Caret.MoveTo(targetPos.Value);
                                _textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(targetPos.Value, 0));
                                e.Handled = true; // 標記事件已處理，避免其他處理器重複處理
                                return;
                            }

                            // 步驟2: 在專案的其他檔案中搜尋標籤
                            var crossFileTarget = FindTagInProject(key);
                            if (crossFileTarget != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"{Constants.Name}: Found tag in project file: {crossFileTarget.FilePath}");
                                NavigateToFile(crossFileTarget.FilePath, crossFileTarget.Line, crossFileTarget.Column);
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{Constants.Name}: Mouse processor error: {ex.Message}");
            }

            // 如果沒有處理事件，則傳遞給基底類別
            base.PreprocessMouseLeftButtonDown(e);
        }

        /// <summary>
        /// 在當前文字緩衝區中搜尋指定的標籤
        /// 逐行掃描文字內容，尋找格式為 // tag:#key 的標籤定義
        /// </summary>
        /// <param name="key">要搜尋的標籤名稱</param>
        /// <returns>如果找到標籤則回傳其位置，否則回傳 null</returns>
        private SnapshotPoint? FindTagInBuffer(string key)
        {
            var snapshot = _textView.TextBuffer.CurrentSnapshot;
            
            // 建立精確的搜尋模式，使用 \b 確保單字邊界匹配
            var targetPattern = $@"//\s*tag:#{Regex.Escape(key)}\b";
            var regex = new Regex(targetPattern, RegexOptions.IgnoreCase);

            // 逐行掃描整個檔案
            for (int i = 0; i < snapshot.LineCount; i++)
            {
                var line = snapshot.GetLineFromLineNumber(i);
                var lineText = line.GetText();

                var match = regex.Match(lineText);
                if (match.Success)
                {
                    // 回傳標籤在檔案中的精確位置
                    return line.Start + match.Index;
                }
            }

            return null;
        }

        /// <summary>
        /// 在整個專案中搜尋指定的標籤
        /// 執行步驟：
        /// 1. 取得當前檔案路徑
        /// 2. 尋找專案根目錄
        /// 3. 在專案目錄中搜尋包含目標標籤的檔案
        /// </summary>
        /// <param name="key">要搜尋的標籤名稱</param>
        /// <returns>如果找到標籤則回傳 TagLocation 物件，包含檔案路徑和位置資訊；否則回傳 null</returns>
        private TagLocation FindTagInProject(string key)
        {
            try
            {
                // 取得當前文件的檔案路徑
                if (!_textView.TextBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var textDocument))
                {
                    return null;
                }
                
                if (textDocument?.FilePath == null)
                {
                    return null;
                }
                
                var currentDir = Path.GetDirectoryName(textDocument.FilePath);
                var projectRoot = FindProjectRoot(currentDir);
                
                if (projectRoot == null) 
                {
                    return null;
                }

                return SearchTagInDirectory(projectRoot, key, textDocument.FilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{Constants.Name}: Project search error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 向上搜尋以找到專案根目錄
        /// 透過檢查以下檔案/目錄來判斷專案根目錄：
        /// - *.csproj, *.vbproj (Visual Studio 專案檔)
        /// - *.sln (Visual Studio 解決方案檔)
        /// - .git 目錄 (Git 儲存庫根目錄)
        /// </summary>
        /// <param name="startDir">開始搜尋的目錄路徑</param>
        /// <returns>專案根目錄路徑，如果找不到則回傳起始目錄</returns>
        private string FindProjectRoot(string startDir)
        {
            var dir = startDir;
            while (dir != null)
            {
                // 檢查是否存在專案相關檔案
                if (Directory.GetFiles(dir, "*.csproj").Any() ||
                    Directory.GetFiles(dir, "*.vbproj").Any() ||
                    Directory.GetFiles(dir, "*.sln").Any() ||
                    Directory.Exists(Path.Combine(dir, ".git")))
                {
                    return dir;
                }
                
                // 向上一層目錄繼續搜尋
                var parent = Directory.GetParent(dir);
                dir = parent?.FullName;
            }
            
            // 如果找不到專案根目錄，回傳起始目錄
            return startDir;
        }

        /// <summary>
        /// 在指定目錄及其子目錄中搜尋包含目標標籤的檔案
        /// 支援多種程式語言的檔案類型，並逐行掃描檔案內容
        /// 支援的檔案類型：.cs, .vb, .js, .ts, .txt, .xml, .html, .css, .cpp, .h, .py, .java
        /// </summary>
        /// <param name="directory">要搜尋的根目錄</param>
        /// <param name="key">要搜尋的標籤名稱</param>
        /// <param name="currentFilePath">當前檔案路徑，用於避免重複搜尋</param>
        /// <returns>如果找到標籤則回傳 TagLocation，否則回傳 null</returns>
        private TagLocation SearchTagInDirectory(string directory, string key, string currentFilePath)
        {
            // 建立精確的標籤搜尋模式
            var targetPattern = $@"//\s*tag:#{Regex.Escape(key)}\b";
            var regex = new Regex(targetPattern, RegexOptions.IgnoreCase);

            // 支援的程式語言檔案副檔名（符合 README.md 中的描述）
            var extensions = new[] { ".cs", ".vb", ".js", ".ts", ".txt", ".xml", ".html", ".css", ".cpp", ".h", ".py", ".java" };

            // 逐一搜尋每種檔案類型
            foreach (var ext in extensions)
            {
                try
                {
                    // 遞迴搜尋指定副檔名的所有檔案
                    var files = Directory.GetFiles(directory, $"*{ext}", SearchOption.AllDirectories);
                    
                    foreach (var file in files)
                    {
                        // 跳過當前檔案，避免重複搜尋
                        if (file.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        try
                        {
                            // 讀取檔案所有行
                            var lines = File.ReadAllLines(file);
                            for (int i = 0; i < lines.Length; i++)
                            {
                                var match = regex.Match(lines[i]);
                                if (match.Success)
                                {
                                    // 找到標籤，回傳位置資訊
                                    return new TagLocation
                                    {
                                        FilePath = file,
                                        Line = i,
                                        Column = match.Index
                                    };
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // 記錄檔案讀取錯誤，但繼續搜尋其他檔案
                            System.Diagnostics.Debug.WriteLine($"{Constants.Name}: File read error {file}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 記錄目錄搜尋錯誤，但繼續搜尋其他檔案類型
                    System.Diagnostics.Debug.WriteLine($"{Constants.Name}: Directory search error for {ext}: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// 導航到指定的檔案和位置
        /// 使用 Visual Studio 的 DTE API 開啟檔案並移動游標到指定位置
        /// 注意：必須在 UI 執行緒上執行
        /// </summary>
        /// <param name="filePath">要開啟的檔案完整路徑</param>
        /// <param name="line">目標行號（從 0 開始）</param>
        /// <param name="column">目標欄位（從 0 開始）</param>
        private void NavigateToFile(string filePath, int line, int column)
        {
            try
            {
                // 確保在 UI 執行緒上執行（DTE API 要求）
                ThreadHelper.ThrowIfNotOnUIThread();
                
                // 取得 Visual Studio 的主要自動化物件
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte != null)
                {
                    // 開啟指定檔案
                    var window = dte.ItemOperations.OpenFile(filePath);
                    if (window?.Document?.Selection is EnvDTE.TextSelection selection)
                    {
                        // Visual Studio 的行號和欄位都是從 1 開始計算
                        selection.GotoLine(line + 1, true); // 移動到指定行
                        selection.MoveToLineAndOffset(line + 1, column + 1); // 精確移動到指定位置
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{Constants.Name}: Navigate to file error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 標籤位置資訊類別
    /// 用於儲存在檔案中找到的標籤位置資訊
    /// 包含檔案路徑、行號和欄位位置
    /// </summary>
    public class TagLocation
    {
        /// <summary>
        /// 包含標籤的檔案完整路徑
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// 標籤所在的行號（從 0 開始計算）
        /// </summary>
        public int Line { get; set; }
        
        /// <summary>
        /// 標籤在該行中的欄位位置（從 0 開始計算）
        /// </summary>
        public int Column { get; set; }
    }
}