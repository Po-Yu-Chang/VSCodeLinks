using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace CodeLinks.Classifier
{
    /// <summary>
    /// tag:#key 分類定義 - 藍色粗體
    /// </summary>
    [Export(typeof(ClassificationTypeDefinition))]
    [Name("CodeLinkTag")]
    internal static class CodeLinkTagClassificationDefinition
    {
    }

    /// <summary>
    /// goto:#key 分類定義 - 綠色粗體
    /// </summary>
    [Export(typeof(ClassificationTypeDefinition))]
    [Name("CodeLinkGoto")]
    internal static class CodeLinkGotoClassificationDefinition
    {
    }

    /// <summary>
    /// tag:#key 格式定義
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "CodeLinkTag")]
    [Name("CodeLinkTag")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class CodeLinkTagFormat : ClassificationFormatDefinition
    {
        public CodeLinkTagFormat()
        {
            DisplayName = "CodeLink Tag";
            ForegroundColor = Colors.Blue;
            IsBold = true;
        }
    }

    /// <summary>
    /// goto:#key 格式定義
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "CodeLinkGoto")]
    [Name("CodeLinkGoto")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class CodeLinkGotoFormat : ClassificationFormatDefinition
    {
        public CodeLinkGotoFormat()
        {
            DisplayName = "CodeLink Goto";
            ForegroundColor = Colors.Green;
            IsBold = true;
        }
    }
}