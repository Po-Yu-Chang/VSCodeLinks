using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace CodeLinks
{
    /// <summary>
    /// 超簡單版本 - 只做標記和基本導航
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(ITextMarkerTag))]
    internal sealed class UltraSimpleTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null) return null;
            return new UltraSimpleTagger(buffer) as ITagger<T>;
        }
    }

    /// <summary>
    /// 超簡單標記器
    /// </summary>
    internal sealed class UltraSimpleTagger : ITagger<ITextMarkerTag>
    {
        private readonly ITextBuffer _buffer;
        private static readonly Regex TagRegex = new Regex(@"//\s*tag:#(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex GotoRegex = new Regex(@"//\s*goto:#(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public UltraSimpleTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            System.Diagnostics.Debug.WriteLine("UltraSimple: Tagger created");
        }

        public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || _buffer == null)
                yield break;

            foreach (var span in spans)
            {
                var text = span.GetText();
                
                if (!text.Contains("tag:#") && !text.Contains("goto:#"))
                    continue;
                
                // tag:#key - 藍色標記
                foreach (Match match in TagRegex.Matches(text))
                {
                    var tagSpan = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                    yield return new TagSpan<ITextMarkerTag>(tagSpan, new TextMarkerTag("blue"));
                }
                
                // goto:#key - 綠色標記
                foreach (Match match in GotoRegex.Matches(text))
                {
                    var tagSpan = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                    yield return new TagSpan<ITextMarkerTag>(tagSpan, new TextMarkerTag("green"));
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }

    /// <summary>
    /// 滑鼠處理器 - 處理雙擊
    /// </summary>
    [Export(typeof(IMouseProcessorProvider))]
    [Name("UltraSimpleMouseProcessor")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class UltraSimpleMouseProcessorProvider : IMouseProcessorProvider
    {
        public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            return new UltraSimpleMouseProcessor(wpfTextView);
        }
    }

    /// <summary>
    /// 滑鼠處理器 - 雙擊觸發導航
    /// </summary>
    internal sealed class UltraSimpleMouseProcessor : MouseProcessorBase
    {
        private readonly IWpfTextView _textView;
        private static readonly Regex GotoRegex = new Regex(@"//\s*goto:#(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public UltraSimpleMouseProcessor(IWpfTextView textView)
        {
            _textView = textView;
        }

        public override void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            try
            {
                // 檢查是否是雙擊
                if (e.ClickCount == 2)
                {
                    System.Diagnostics.Debug.WriteLine("UltraSimple: Double click detected");

                    var position = _textView.Caret.Position.BufferPosition;
                    var line = position.GetContainingLine();
                    var lineText = line.GetText();

                    System.Diagnostics.Debug.WriteLine($"UltraSimple: Line text: {lineText}");

                    // 檢查是否點擊在 goto 行上
                    if (lineText.Contains("goto:#"))
                    {
                        var matches = GotoRegex.Matches(lineText);
                        foreach (Match match in matches)
                        {
                            var key = match.Groups[1].Value;
                            System.Diagnostics.Debug.WriteLine($"UltraSimple: Found goto key: {key}");

                            // 嘗試找到對應的 tag
                            var targetPos = FindTagInBuffer(key);
                            if (targetPos.HasValue)
                            {
                                System.Diagnostics.Debug.WriteLine($"UltraSimple: Navigating to tag: {key}");
                                _textView.Caret.MoveTo(targetPos.Value);
                                _textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(targetPos.Value, 0));
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UltraSimple: Mouse processor error: {ex.Message}");
            }

            base.PreprocessMouseLeftButtonDown(e);
        }

        private SnapshotPoint? FindTagInBuffer(string key)
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
    }
}