using System;

namespace TestProject
{
    public class TestClass
    {
        // tag:#MainMethod
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            CallHelper(); 
            // goto:#Helper
        }

        // tag:#Helper
        private static void CallHelper()
        {
            // Some logic here
            Console.WriteLine("In Helper Method");
            // goto:#MainMethod  
        }
        
        public void AnotherMethod()
        {
            // goto:#Helper
            // goto:#MainMethod     
        }
    }
}