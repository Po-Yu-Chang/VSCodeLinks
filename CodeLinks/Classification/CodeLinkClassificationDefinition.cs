using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using CodeLinks.Tagging;

namespace CodeLinks.Classification
{
    /// <summary>
    /// 程式碼連結分類格式定義
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = CodeLinkTag.TagType)]
    [Name("CodeLink")]
    [UserVisible(true)]
    internal sealed class CodeLinkFormat : ClassificationFormatDefinition
    {
        public CodeLinkFormat()
        {
            DisplayName = "CodeLink";
            ForegroundColor = Colors.SteelBlue; // 藍色前景
            IsBold = true; // 粗體
        }
    }

    /// <summary>
    /// 程式碼連結分類型別定義
    /// </summary>
    [Export(typeof(ClassificationTypeDefinition))]
    [Name(CodeLinkTag.TagType)]
    internal static class CodeLinkClassificationTypeDefinition
    {
        // 這個欄位會自動由 MEF 初始化
        #pragma warning disable 649
        [Export]
        [Name(CodeLinkTag.TagType)]
        [BaseDefinition("text")]
        internal static ClassificationTypeDefinition CodeLinkType;
        #pragma warning restore 649
    }
}
