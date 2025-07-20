using System;

namespace TestProject
{
    public class AnotherClass
    {
        // tag:#AnotherEntry
        public void Start()
        {
            Console.WriteLine("Another Entry Point");
            // goto:#Helper
        }

        // tag:#CrossFileTarget
        public void Process()
        {
            // goto:#MainMethod
            Console.WriteLine("Processing...");
        }

        public void TestCrossFile()
        {
            // goto:#AnotherEntry
            // goto:#CrossFileTarget
        }
    }
}