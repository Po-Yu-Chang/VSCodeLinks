using System;

namespace TestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("測試 CodeLinks 擴充套件");
            
            // tag:#main_start
            Console.WriteLine("主程式開始");
            
            ProcessData();
            
            // tag:#main_end
            Console.WriteLine("主程式結束");
        }
        
        static void ProcessData()
        {
            // goto:#main_start
            Console.WriteLine("處理資料");
            
            // tag:#process_data
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"處理項目 {i}");
            }
            
            // goto:#main_end
            Console.WriteLine("資料處理完成");
        }
        
        static void TestMethod()
        {
            // goto:#process_data
            Console.WriteLine("測試方法");
        }
    }
}
