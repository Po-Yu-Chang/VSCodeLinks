/// <summary>
/// 跨檔案跳轉測試檔案
/// 
/// 此檔案展示 CodeLinks 的跨檔案導航功能：
/// - 從 TestFile.cs 可以跳轉到此檔案的標籤
/// - 從此檔案也可以跳轉回 TestFile.cs 的標籤
/// - 展示專案範圍的智慧搜尋功能
/// </summary>

using System;

namespace TestProject
{
    public class AnotherClass
    {
        // tag:#AnotherEntry
        public void Start()
        {
            Console.WriteLine("Another Entry Point");
            // goto:#Helper  ← 跳轉到 TestFile.cs 中的 Helper 方法
        }

        // tag:#CrossFileTarget
        public void Process()
        {
            // goto:#MainMethod  ← 跳轉到 TestFile.cs 中的主方法
            Console.WriteLine("Processing...");
        }

        public void TestCrossFile()
        {
            // goto:#AnotherEntry
            // goto:#CrossFileTarget
            // goto:#MainMethod  ← 展示跨檔案跳轉
        }
    }
}