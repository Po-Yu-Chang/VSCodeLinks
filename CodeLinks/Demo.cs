using System;

namespace CodeLinks.Demo
{
    /// <summary>
    /// CodeLinks 功能展示範例
    /// 這個檔案展示如何使用 // tag:#... 和 // goto:#... 功能
    /// </summary>
    public class DemoClass
    {
        // tag:#MainMethod
        public static void Main(string[] args)
        {
            Console.WriteLine("CodeLinks 展示程式");
            
            // goto:#HelperMethod
            HelperMethod();
            
            // goto:#ComplexCalculation
            var result = ComplexCalculation(10, 20);
            Console.WriteLine($"計算結果: {result}");
        }

        // tag:#HelperMethod  
        private static void HelperMethod()
        {
            Console.WriteLine("這是輔助方法");
            
            // goto:#MainMethod - 可以跳回主要方法
        }

        // tag:#ComplexCalculation
        private static int ComplexCalculation(int a, int b)
        {
            // 這裡進行複雜的計算
            int result = a * b + (a - b);
            
            // goto:#HelperMethod - 也可以跳到其他方法
            return result;
        }

        // tag:#PropertyExample
        public string Name { get; set; }

        public void UseProperty()
        {
            // goto:#PropertyExample
            Name = "CodeLinks 測試";
        }

        // tag:#EventHandlerExample
        public event EventHandler SomeEvent;

        protected virtual void OnSomeEvent()
        {
            // goto:#EventHandlerExample
            SomeEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 另一個類別展示跨類別跳轉
    /// </summary>
    public class AnotherClass
    {
        public void CallMainMethod()
        {
            // goto:#MainMethod - 可以跳轉到其他類別的方法
            DemoClass.Main(new string[0]);
        }

        // tag:#AnotherClassMethod
        public void SomeMethod()
        {
            Console.WriteLine("另一個類別的方法");
        }

        public void CallAnotherMethod()
        {
            // goto:#AnotherClassMethod
            SomeMethod();
        }
    }
}
