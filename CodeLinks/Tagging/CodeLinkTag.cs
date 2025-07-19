using Microsoft.VisualStudio.Text.Tagging;

namespace CodeLinks.Tagging
{
    /// <summary>
    /// 表示程式碼連結標記的類別
    /// </summary>
    internal sealed class CodeLinkTag : TextMarkerTag
    {
        public const string TagType = "CodeLink";
        public const string KindTag = "tag";
        public const string KindGoto = "goto";

        /// <summary>
        /// 取得標記的鍵值
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 取得標記的種類 (tag 或 goto)
        /// </summary>
        public string Kind { get; }

        /// <summary>
        /// 判斷是否為目標標記 (tag)
        /// </summary>
        public bool IsTarget => Kind == KindTag;

        /// <summary>
        /// 建立新的程式碼連結標記
        /// </summary>
        /// <param name="kind">標記種類 (tag 或 goto)</param>
        /// <param name="key">標記鍵值</param>
        public CodeLinkTag(string kind, string key) : base("blue")
        {
            Kind = kind;
            Key = key;
        }
    }
}
