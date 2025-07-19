using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace CodeLinks.Tagging
{
    /// <summary>
    /// 程式碼連結標記器提供者
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("CSharp")]
    [TagType(typeof(CodeLinkTag))]
    internal sealed class CodeLinkTaggerProvider : ITaggerProvider
    {
        [Import] 
        internal IClassificationTypeRegistryService ClassificationRegistry { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                () => new CodeLinkTagger(buffer, ClassificationRegistry)) as ITagger<T>;
        }
    }

    /// <summary>
    /// 程式碼連結標記器實作
    /// </summary>
    internal sealed class CodeLinkTagger : ITagger<CodeLinkTag>
    {
        // 比對 // tag:#key 或 // goto:#key 的正規表達式
        private readonly Regex _regex = new Regex(@"//\s*(?<kind>tag|goto):#?(?<key>[A-Za-z][\w]*)",
                                                  RegexOptions.Compiled);
        private readonly ITextBuffer _buffer;
        private readonly IClassificationType _classType;

        public CodeLinkTagger(ITextBuffer buffer, IClassificationTypeRegistryService registry)
        {
            _buffer = buffer;
            _classType = registry.GetClassificationType("comment");
            _buffer.Changed += OnBufferChanged;
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // 當文字緩衝區變更時觸發標記重新整理
            var snapshot = e.After;
            var span = new SnapshotSpan(snapshot, 0, snapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        public IEnumerable<ITagSpan<CodeLinkTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                var text = span.GetText();
                foreach (Match match in _regex.Matches(text))
                {
                    var kind = match.Groups["kind"].Value;
                    var key = match.Groups["key"].Value;
                    var tagSpan = new SnapshotSpan(span.Snapshot, 
                                                   span.Start + match.Index, 
                                                   match.Length);
                    yield return new TagSpan<CodeLinkTag>(tagSpan, new CodeLinkTag(kind, key));
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
