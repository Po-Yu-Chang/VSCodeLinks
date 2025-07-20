/// <summary>
/// CodeLinks 功能測試檔案
/// 
/// 此檔案示範 CodeLinks 擴充功能的基本用法：
/// - // tag:#標籤名稱 定義標籤位置（藍色標記）
/// - // goto:#標籤名稱 建立跳轉連結（綠色標記）
/// - 雙擊 goto 行即可跳轉到對應的 tag 位置
/// </summary>

using System;

namespace TestProject
{
    public class TestClass
    {
        // tag:#MainMethod
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            // goto:#Helper  ← 雙擊這行可跳轉到下面的 Helper 方法
        }

        // tag:#Helper
        private static void CallHelper()
        {
            Console.WriteLine("Helper method called!");
            // goto:#MainMethod  ← 可以跳回主方法
        }
        
        // tag:#AnotherMethod
        public void AnotherMethod()
        {
            // 展示多個跳轉選項
            // goto:#Helper
            // goto:#MainMethod
            // goto:#CrossFileTarget  ← 這個標籤在 TestFile2.cs 中
        }
    }
}