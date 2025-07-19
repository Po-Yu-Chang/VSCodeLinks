using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using CodeLinks.Tagging;

namespace CodeLinks.Navigation
{
    /// <summary>
    /// 程式碼連結邊緣提供者
    /// </summary>
    [Export(typeof(IWpfTextViewMarginProvider))]
    [MarginContainer(PredefinedMarginNames.Right)]
    [Name("CodeLinkMargin")]
    [Order(After = PredefinedMarginNames.VerticalScrollBar)]
    [ContentType("CSharp")]
    internal sealed class CodeLinkMarginProvider : IWpfTextViewMarginProvider
    {
        [Import] 
        internal IViewTagAggregatorFactoryService TagAggregatorFactory { get; set; }
        
        [Import] 
        internal SVsServiceProvider ServiceProvider { get; set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost host, IWpfTextViewMargin container)
        {
            var uiShell = ServiceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
            return new CodeLinkMargin(host.TextView, TagAggregatorFactory, uiShell);
        }
    }

    /// <summary>
    /// 程式碼連結邊緣實作
    /// </summary>
    internal sealed class CodeLinkMargin : Canvas, IWpfTextViewMargin
    {
        private readonly IWpfTextView _textView;
        private readonly ITagAggregator<CodeLinkTag> _tagAggregator;
        private readonly IVsUIShell _uiShell;
        private bool _isDisposed = false;

        public CodeLinkMargin(IWpfTextView textView, IViewTagAggregatorFactoryService tagAggFactory, IVsUIShell uiShell)
        {
            _textView = textView;
            _tagAggregator = tagAggFactory.CreateTagAggregator<CodeLinkTag>(textView);
            _uiShell = uiShell;

            // 設定邊緣樣式
            Width = 20;
            Background = Brushes.Transparent;
            ClipToBounds = true;

            // 監聽事件
            _tagAggregator.TagsChanged += OnTagsChanged;
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.ViewportHeightChanged += OnViewportChanged;

            // 初始繪製
            RedrawMargins();
        }

        private void OnTagsChanged(object sender, TagsChangedEventArgs e)
        {
            if (!_isDisposed)
            {
                RedrawMargins();
            }
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (!_isDisposed)
            {
                RedrawMargins();
            }
        }

        private void OnViewportChanged(object sender, EventArgs e)
        {
            if (!_isDisposed)
            {
                RedrawMargins();
            }
        }

        private void RedrawMargins()
        {
            if (_isDisposed) return;

            try
            {
                Children.Clear();

                if (_textView.TextViewLines == null) return;

                foreach (var mapping in _tagAggregator.GetTags(_textView.TextViewLines.FormattedSpan))
                {
                    // 只為 goto 標記建立邊緣元素
                    if (mapping.Tag.Kind == CodeLinkTag.KindGoto)
                    {
                        CreateMarginElement(mapping);
                    }
                }
            }
            catch
            {
                // 忽略繪製錯誤
            }
        }

        private void CreateMarginElement(IMappingTagSpan<CodeLinkTag> mapping)
        {
            try
            {
                var spans = mapping.Span.GetSpans(_textView.TextSnapshot);
                if (spans.Count == 0) return;

                var span = spans[0];
                var geometry = _textView.TextViewLines.GetMarkerGeometry(span);
                if (geometry == null) return;

                // 建立藍色箭頭矩形
                var rect = new Rectangle
                {
                    Width = 10,
                    Height = Math.Max(geometry.Bounds.Height, 14),
                    Fill = new SolidColorBrush(Colors.SteelBlue),
                    Stroke = new SolidColorBrush(Colors.DarkBlue),
                    StrokeThickness = 1,
                    ToolTip = $"跳轉到: {mapping.Tag.Key}",
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // 設定位置
                var top = geometry.Bounds.Top - _textView.ViewportTop;
                SetTop(rect, top);
                SetLeft(rect, 2);

                // 新增點擊事件
                rect.MouseLeftButtonUp += (s, e) => 
                {
                    try
                    {
                        CodeLinkJumpService.JumpTo(_textView, mapping.Tag.Key);
                    }
                    catch
                    {
                        // 忽略跳轉錯誤
                    }
                };

                Children.Add(rect);
            }
            catch
            {
                // 忽略單一元素建立錯誤
            }
        }

        #region IWpfTextViewMargin 實作

        public FrameworkElement VisualElement => this;

        public double MarginSize => ActualWidth;

        public bool Enabled => !_isDisposed;

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Equals(marginName, "CodeLinkMargin", StringComparison.OrdinalIgnoreCase) ? this : null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                
                // 清理事件處理器
                if (_tagAggregator != null)
                {
                    _tagAggregator.TagsChanged -= OnTagsChanged;
                    _tagAggregator.Dispose();
                }

                if (_textView != null)
                {
                    _textView.LayoutChanged -= OnLayoutChanged;
                    _textView.ViewportHeightChanged -= OnViewportChanged;
                }

                Children.Clear();
            }
        }

        #endregion
    }
}
