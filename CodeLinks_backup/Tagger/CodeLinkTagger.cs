using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace CodeLinks.Tagger
{
    /// <summary>
    /// 程式碼連結標記提供者
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("CSharp")]
    [TagType(typeof(IClassificationTag))]
    internal sealed class CodeLinkTaggerProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null) return null;
            
            System.Diagnostics.Debug.WriteLine("CodeLinks: Creating CodeLinkTagger");
            return new CodeLinkTagger(buffer, ClassificationTypeRegistry) as ITagger<T>;
        }
    }

    /// <summary>
    /// 程式碼連結標記器 - 提供語法高亮
    /// </summary>
    internal sealed class CodeLinkTagger : ITagger<IClassificationTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly IClassificationTypeRegistryService _classificationTypeRegistry;
        private readonly Regex _tagRegex;
        private readonly Regex _gotoRegex;

        public CodeLinkTagger(ITextBuffer buffer, IClassificationTypeRegistryService classificationTypeRegistry)
        {
            _buffer = buffer;
            _classificationTypeRegistry = classificationTypeRegistry;
            _tagRegex = new Regex(@"//\s*tag:#\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _gotoRegex = new Regex(@"//\s*goto:#\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            System.Diagnostics.Debug.WriteLine("CodeLinks: CodeLinkTagger created");
        }

        public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || _buffer == null)
                yield break;

            foreach (var span in spans)
            {
                var text = span.GetText();
                
                // 檢查 tag:#key - 藍色粗體
                foreach (Match match in _tagRegex.Matches(text))
                {
                    var tagSpan = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                    var classificationType = _classificationTypeRegistry.GetClassificationType("CodeLinkTag");
                    yield return new TagSpan<IClassificationTag>(tagSpan, new ClassificationTag(classificationType));
                }
                
                // 檢查 goto:#key - 綠色粗體
                foreach (Match match in _gotoRegex.Matches(text))
                {
                    var tagSpan = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                    var classificationType = _classificationTypeRegistry.GetClassificationType("CodeLinkGoto");
                    yield return new TagSpan<IClassificationTag>(tagSpan, new ClassificationTag(classificationType));
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}